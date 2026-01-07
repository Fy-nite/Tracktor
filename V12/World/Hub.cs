using System;
using System.Collections.Generic;
using System.Text;

namespace V12.World
{

    public static class Hub
    {
        // Create a hub-style world prepopulated with useful elements and components.
        // When running headless, pass headless=true to avoid creating Godot engine objects.
        public static V12.V12World CreateHubWorld(string name = "HubWorld", bool headless = false)
        {
            var world = new V12.V12World(name, headless: headless);

            // Root element
            var root = world.RootElement;
            root.Name = "HubRoot";

            // Floor
            var floor = new V12.Types.Element("Floor")
            {
                Description = "Central hub floor",
                Position = new V12.Types.DoubleVector3(0, 0, 0, 0),
                Rotation = new V12.Types.DoubleVector3(0, 0, 0, 0),
                Scale = new V12.Types.DoubleVector3(100, 1, 100, 0)
            };
            floor.AddComponent(new V12.Components.BoxMesh(new Godot.Vector3(0, -0.5f, 0), Godot.Vector3.Zero, new Godot.Vector3(100, 1, 100)));

            // Lighting
            var sun = new V12.Types.Element("Sun") { Description = "Directional light for the hub" };
            sun.AddComponent(new V12.Components.DirectionalLight { Position = new Godot.Vector3(0, 10, 0), Rotation = new Godot.Vector3(-45, 0, 0), Energy = 0.8f });

            // Sky / environment
            var sky = new V12.Types.Element("SkyBox") { Description = "Panorama sky for hub" };
            sky.AddComponent(new V12.Components.PanoramaSky { TexturePath = "res://Assets/Day.hdr", Size = 500f });

            // Spawn / Desktop player element (local players will instantiate their own player)
            var spawn = new V12.Types.Element("SpawnPoint") { Description = "Default spawn point for visitors" };
            spawn.AddComponent(new V12.Components.DesktopPlayer { Position = new Godot.Vector3(0, 2, 0), Rotation = Godot.Vector3.Zero, Scale = new Godot.Vector3(1, 1, 1), MoveSpeed = 4.0f });

            // In-world inspector so users can inspect hub elements
            var inspectorEl = new V12.Types.Element("Hub Inspector") { Description = "Inspect hub elements" };
            inspectorEl.AddComponent(new V12.Components.Inspector { Position = new Godot.Vector3(2, 1, 0), Rotation = Godot.Vector3.Zero, Scale = new Godot.Vector3(0.6f, 0.6f, 0.6f) });

            // Sample info board
            var info = new V12.Types.Element("InfoBoard") { Description = "Information for visitors" };
            info.AddComponent(new V12.Components.BoxMesh { Position = new Godot.Vector3(3, 1, 0), Rotation = Godot.Vector3.Zero, Scale = new Godot.Vector3(2, 1, 0.2f) });

            // Assemble hierarchy
            root.AddChild(floor);
            root.AddChild(sun);
            root.AddChild(sky);
            root.AddChild(spawn);
            root.AddChild(inspectorEl);
            root.AddChild(info);

            // World flags
            world.IsLocalOnly = false;
            world.CanBeLeft = true;
            world.CanBeClosed = true;

            return world;
        }
    }
}
