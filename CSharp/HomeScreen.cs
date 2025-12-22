using Godot;
using Trakction.XR;

public partial class HomeScreen : Node
{
    [Export] public PackedScene XrBootstrapScene { get; set; }
    [Export] public PackedScene XrPlayerRigScene { get; set; }
    [Export] public string StartVrAction { get; set; } = "ui_accept";

    private Node mainGame;
    private NetRoot? _net;
    private bool _vrStarted;

    public override void _Ready()
    {
        mainGame = GetNode("MainGame");
        _net = GetNode<NetRoot>("NetRoot");

        // Connect VR button signal
        var vrButton = GetNode<Button>("UI/NetConnect/VBox/Buttons/VrButton");
        vrButton.Pressed += OnVrButtonPressed;

        // Connect Spawn Box button (server-only action)
        var spawnBtn = GetNode<Button>("UI/NetConnect/VBox/Buttons/SpawnBox");
        spawnBtn.Pressed += OnSpawnBoxPressed;
        // Only show spawn button when running as server
        spawnBtn.Visible = _net != null && _net.IsServerMode;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_vrStarted)
            return;

        if (@event.IsActionPressed(StartVrAction))
        {
            // Avoid triggering while typing into a LineEdit
            var focus = GetFocusOwner();
            if (focus is LineEdit)
                return;

            OnVrButtonPressed();
        }
    }

    private void OnVrButtonPressed()
    {
        if (_vrStarted)
            return;
        _vrStarted = true;
        // Instantiate and add OpenXrBootstrap
        if (XrBootstrapScene != null)
        {
            var bootstrap = XrBootstrapScene.Instantiate<Node>();
            AddChild(bootstrap);
        }

        // If not in multiplayer, instantiate VR rig locally
        if (Multiplayer.MultiplayerPeer == null)
        {
            if (XrPlayerRigScene != null)
            {
                var rig = XrPlayerRigScene.Instantiate<Node3D>();
                if (rig is XrPlayerRig xrRig)
                {
                    xrRig.IsLocalPlayer = true;
                    xrRig.PeerId = 1; // Local peer
                }
                mainGame.AddChild(rig);
            }
        }
        else
        {
            // Notify server to switch to VR player
            _net?.StartVr();
        }

        // Optionally hide UI or transition
        GetNode<CanvasLayer>("UI").Visible = false;
    }

    private void OnSpawnBoxPressed()
    {
        if (_net == null)
            return;

        // Ask the server instance to spawn a box above its player.
        _net.ServerSpawnBoxAboveSelf();
    }
}