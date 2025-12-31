using Godot;
using V12.Interfaces;
using V12.World;
using V12.World.Templates;
using V12.Types;
using V12.UI;

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

        public V12World(string name)
        {
            Name = name;
            World3D = new World3D();
            Viewport = new Node3D();
            RootElement = new Element { Name = "Root" };
            Syncer = new WorldElementSceneSyncer(RootElement, Viewport);
        }

        public void GenerateWorld()
        {
            var Dash = Dashboard.Generate();
            RootElement.AddChildElement(Dash);
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