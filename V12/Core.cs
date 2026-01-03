using Godot;
namespace V12
{
    public class V12Engine
    {
        public List<V12World> Worlds { get; set; } = new();
        public List<Action> LoopCallbacks { get; set; }
        public bool IsEngineRunning { get; set; } = false;
        public double Delta;

        public V12Engine()
        {
            LoopCallbacks = new List<Action>();
        }

        // Adds a world to the engine and optionally attaches its viewport to a scene node
        public void AddWorld(V12World world, Node parent = null)
        {
            if (world == null) return;
            Worlds.Add(world);
            // If a parent is provided, parent the world's viewport into the scene immediately
            // but keep it hidden until the world is activated. This avoids reparenting later.
            if (parent != null)
            {

                if (world.Viewport.GetParent() != parent)
                {
                    // name the world root so that we know what it is in the scene tree
                    parent.AddChild(world.Viewport);
                    world.Viewport.Name = $"WorldViewport_{world.Name}";

                }
                // Ensure it's not visible/processing until activated
                world.InternalSetVisible(false);
                world.InternalSetProcessing(false);
            }
        }

        // Activate a world by index. This will deactivate the current active world (if any) and
        // activate the requested world, reparenting their viewports appropriately under the provided parent.
        public void ActivateWorld(int index, Node parent)
        {
            if (index < 0 || index >= Worlds.Count) return;

            // Deactivate any currently active world that can be left
            for (int i = 0; i < Worlds.Count; i++)
            {
                var w = Worlds[i];
                if (w.IsActive && w.CanBeLeft)
                {
                    w.Deactivate();
                }
            }

            var target = Worlds[index];
            target.Activate(parent);
        }

        public void Update(double delta)
        {
            Delta = delta;
            // Update all worlds
            foreach (var world in Worlds)
            {
                if (world == null) continue;

                // Only update worlds that are active or local-only (UI/user-space)
                if (world.IsActive || world.IsLocalOnly)
                {
                    world.Update(delta);
                }
            }
            // Execute all registered loop callbacks for the engine frame update
            foreach (var callback in LoopCallbacks)
            {
                callback();
            }
        }

        public void Run()
        {
            GD.Print("Engine started");
            foreach (var world in Worlds)
            {
                if (world != null)
                {

                }
                GD.Print("World loaded: " + world.Name);
            }
            IsEngineRunning = true;
        }

        public void Stop()
        {
            IsEngineRunning = false;
        }
    }
}
