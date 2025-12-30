using Godot;
using System;

namespace Trakction.XR
{
    /// <summary>
    /// Simple wrapper Node3D that loads/instantiates the VR player as a child.
    /// Exposes IsLocalPlayer and PeerId so C# code can set them before/after instantiation.

    /// </summary>
    public partial class XrPlayerRig : Node3D
    {
        [Export] public PackedScene PlayerScene { get; set; }

        // These are simple fields the HomeScreen can set. You can use them inside the instantiated player
        // if you expose/propagate them from the instantiated node (for GDScript scenes you'd need to call
        // methods or set properties on the child after instantiation).
        private bool _isLocalPlayer;
        public bool IsLocalPlayer 
        { 
            get => _isLocalPlayer; 
            set 
            {
                _isLocalPlayer = value;
                OnIsLocalPlayerChanged();
            }
        }
        public long PeerId { get; set; }

        private Node3D? _playerInstance;

        private void OnIsLocalPlayerChanged()
        {
             PropagatePlayerFlags();
             if (_playerInstance == null) return;

             if (_isLocalPlayer)
             {
                 // Add interaction setup if not present
                 if (!_playerInstance.HasNode("PlayerInteractionSetup"))
                 {
                     var setup = new Trakction.Interaction.PlayerInteractionSetup();
                     setup.Name = "PlayerInteractionSetup";
                     _playerInstance.AddChild(setup);
                 }
                 
                 // Ensure camera is current
                 var cam = _playerInstance.FindChild("XRCamera3D", true, false) as Camera3D;
                 if (cam != null) cam.Current = true;
             }
             else
             {
                 // Remote Player Setup
                 // Disable camera
                 var cam = _playerInstance.FindChild("XRCamera3D", true, false) as Camera3D;
                 if (cam != null) cam.Current = false;
                 
                 // Disable processing (physics/input) to prevent local movement logic from running on remote instances
                 _playerInstance.SetProcess(false);
                 _playerInstance.SetPhysicsProcess(false);
                 _playerInstance.SetProcessInput(false);
                 _playerInstance.SetProcessUnhandledInput(false);
             }
        }

        public override void _Ready()
        {
            // Instantiate the player scene if provided. Keep the wrapper so we can reuse or hide instead
            // of freeing constantly.
            if (PlayerScene != null)
            {
                var inst = PlayerScene.Instantiate();
                if (inst is Node3D n3d)
                {
                    _playerInstance = n3d;
                    AddChild(n3d);
                    
                    // Setup Synchronizer for the child (the actual player body)
                    var sync = new MultiplayerSynchronizer();
                    sync.Name = "MultiplayerSynchronizer";
                    n3d.AddChild(sync);
                    
                    var config = new SceneReplicationConfig();
                    config.AddProperty(":position");
                    config.AddProperty(":rotation");
                    sync.ReplicationConfig = config;

                    // Try to propagate the IsLocalPlayer/PeerId values to the instantiated node.
                    // For GDScript scenes, you can call methods or set exported properties using Call or Set.
                    // We'll attempt both common patterns in a best-effort, non-throwing way.
                    PropagatePlayerFlags();
                }
                else
                {
                    // If it's not Node3D, just add it and keep a reference as Node
                    AddChild(inst);
                }
            }
            
            // Auto-detect authority
            PeerId = GetMultiplayerAuthority();
            IsLocalPlayer = PeerId == Multiplayer.GetUniqueId();
        }

        public void Initialize(bool isLocal, int peerId)
        {
            IsLocalPlayer = isLocal;
            PeerId = peerId;
            PropagatePlayerFlags();
        }

        private void PropagatePlayerFlags()
        {
            if (_playerInstance == null)
                return;

            // 1) Try common C#-style properties (works if the instance has a C# script with exposed members)
            try
            {
                // Use Set to set exported properties by name (works for GDScript exported props too).
                _playerInstance.Set("IsLocalPlayer", IsLocalPlayer);
                _playerInstance.Set("PeerId", PeerId);
            }
            catch
            {
                // ignore failures - not all scenes expose those properties
            }

            // 2) Try calling a well-known method if present on the player scene (GDScript or other script)
            try
            {
                if (_playerInstance.HasMethod("initialize_for_local_player"))
                {
                    _playerInstance.Call("initialize_for_local_player", IsLocalPlayer, PeerId);
                }
                else if (_playerInstance.HasMethod("initialize"))
                {
                    _playerInstance.Call("initialize", IsLocalPlayer, PeerId);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
