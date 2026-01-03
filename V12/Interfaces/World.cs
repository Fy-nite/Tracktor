using Godot;
using V12.Types;

namespace V12
{
    public class V12World
    {
        public string Name { get; set; }
        public World3D World3D { get; private set; }
        public Node3D Viewport { get; private set; }
        public Element RootElement { get; private set; }
        public WorldElementSceneSyncer Syncer { get; private set; }
        public List<Action> LoopCallbacks { get; set; } = new();
        public bool IsLocalOnly { get; set; } = false; // For worlds like UserSpace that aren't synced
        public bool CanBeLeft { get; set; } = true; // Whether the world can be switched away from
        public bool CanBeClosed { get; set; } = true; // Whether the world can be deleted/disposed
        public bool IsActive { get; private set; } = false;

        public V12World(string name)
        {
            Name = name;
            World3D = new World3D();
            Viewport = new Node3D();
            RootElement = new Element { Name = "Root" };
            Syncer = new WorldElementSceneSyncer(RootElement, Viewport);
        }

        public void Activate(Node parent = null)
        {
            if (IsActive) return;
            if (parent != null && Viewport.GetParent() != parent)
            {
                parent.AddChild(Viewport);
            }
            // Make node visible and enable processing so the world becomes active
            InternalSetVisible(true);
            InternalSetProcessing(true);
            IsActive = true;
            // Register as current world
            WorldManager.SetCurrent(this);
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            // Don't remove local-only worlds when deactivating; they should remain visible
            if (IsLocalOnly) return;
            // Hide and stop processing but keep node in the scene tree
            InternalSetVisible(false);
            InternalSetProcessing(false);
            IsActive = false;
            // If this world was the current, clear it
            if (WorldManager.CurrentWorld == this)
            {
                WorldManager.ClearCurrent();
            }
        }

        // Internal helpers to toggle visibility and processing on the root viewport node.
        public void InternalSetVisible(bool visible)
        {
            try
            {
                Viewport.Visible = visible;
            }
            catch
            {
                // If Viewport doesn't expose Visible directly, fall back to calling Show/Hide on Controls
                if (visible)
                {
                    (Viewport as Node)?.CallDeferred("show");
                }
                else
                {
                    (Viewport as Node)?.CallDeferred("hide");
                }
            }
        }

        public void InternalSetProcessing(bool processing)
        {
            Viewport.SetProcess(processing);
            Viewport.SetPhysicsProcess(processing);
            Viewport.SetProcessUnhandledInput(processing);
            Viewport.SetProcessUnhandledKeyInput(processing);
        }

        public void GenerateWorld()
        {

        }

        public void Update(double delta)
        {
            // World-specific update logic
            foreach (var callback in LoopCallbacks)
            {
                callback();
            }
        }
    }
}