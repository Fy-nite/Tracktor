using Godot;
using System.Collections.Generic;
using V12;
using V12.Interfaces;
using V12.Types;
using V12.Components;

public static class GeneratedScene
{
    public static void Build(V12World world)
    {
        if (world == null) throw new System.ArgumentNullException(nameof(world));
        var root = world.RootElement;

        var elem0 = new Element
        {
            Name = "VRPlayer",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "VRPlayer", NodeTypeName = "Godot.Node3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        root.AddChildElement(elem0);

        var elem1 = new Element
        {
            Name = "Glasses",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "Glasses", NodeTypeName = "Godot.SubViewport, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem0.AddChildElement(elem1);

        var elem2 = new Element
        {
            Name = "CharacterBody3D",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "CharacterBody3D", NodeTypeName = "Godot.CharacterBody3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem1.AddChildElement(elem2);

        var elem3 = new Element
        {
            Name = "CollisionShape3D",
            Position = new DoubleVector3(0d, 1d, 0d, 0d),
            Rotation = new DoubleVector3(-0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "CollisionShape3D", NodeTypeName = "Godot.CollisionShape3D, GodotSharp", Position = new Vector3(0f, 1f, 0f), Rotation = new Vector3(-0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem2.AddChildElement(elem3);

        var elem4 = new Element
        {
            Name = "XROrigin3D",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "XROrigin3D", NodeTypeName = "Godot.XROrigin3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem2.AddChildElement(elem4);

        var elem5 = new Element
        {
            Name = "XRCamera3D",
            Position = new DoubleVector3(0d, 1.5d, -0.1d, 0d),
            Rotation = new DoubleVector3(-0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "XRCamera3D", NodeTypeName = "Godot.XRCamera3D, GodotSharp", Position = new Vector3(0f, 1.5f, -0.1f), Rotation = new Vector3(-0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem4.AddChildElement(elem5);

        var elem6 = new Element
        {
            Name = "Neck",
            Position = new DoubleVector3(0d, -0.1d, 0.1d, 0d),
            Rotation = new DoubleVector3(-0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "Neck", NodeTypeName = "Godot.Node3D, GodotSharp", Position = new Vector3(0f, -0.1f, 0.1f), Rotation = new Vector3(-0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem5.AddChildElement(elem6);

        var elem7 = new Element
        {
            Name = "LeftHand",
            Position = new DoubleVector3(-0.5d, 1d, -0.5d, 0d),
            Rotation = new DoubleVector3(-0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "LeftHand", NodeTypeName = "Godot.XRController3D, GodotSharp", Position = new Vector3(-0.5f, 1f, -0.5f), Rotation = new Vector3(-0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem4.AddChildElement(elem7);

        var elem8 = new Element
        {
            Name = "PlaceholderHand",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "PlaceholderHand", NodeTypeName = "Godot.MeshInstance3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem7.AddChildElement(elem8);

        var elem9 = new Element
        {
            Name = "LeftLazer",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "LeftLazer", NodeTypeName = "Godot.RayCast3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem7.AddChildElement(elem9);

        var elem10 = new Element
        {
            Name = "LeftLazerMesh",
            Position = new DoubleVector3(0d, 0d, -0.081741d, 0d),
            Rotation = new DoubleVector3(-1.570796d, -0d, 0d, 0d),
            Scale = new DoubleVector3(0.05602d, 0.05602d, 0.05602d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "LeftLazerMesh", NodeTypeName = "Godot.MeshInstance3D, GodotSharp", Position = new Vector3(0f, 0f, -0.081741f), Rotation = new Vector3(-1.570796f, -0f, 0f), Scale = new Vector3(0.05602f, 0.05602f, 0.05602f) }
            }
        };
        elem9.AddChildElement(elem10);

        var elem11 = new Element
        {
            Name = "RightHand",
            Position = new DoubleVector3(0.5d, 1d, -0.5d, 0d),
            Rotation = new DoubleVector3(-0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "RightHand", NodeTypeName = "Godot.XRController3D, GodotSharp", Position = new Vector3(0.5f, 1f, -0.5f), Rotation = new Vector3(-0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem4.AddChildElement(elem11);

        var elem12 = new Element
        {
            Name = "PlaceholderHand",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "PlaceholderHand", NodeTypeName = "Godot.MeshInstance3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem11.AddChildElement(elem12);

        var elem13 = new Element
        {
            Name = "RightLazer",
            Position = new DoubleVector3(0d, 0d, 0d, 0d),
            Rotation = new DoubleVector3(0d, 0d, 0d, 0d),
            Scale = new DoubleVector3(1d, 1d, 1d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "RightLazer", NodeTypeName = "Godot.RayCast3D, GodotSharp", Position = new Vector3(0f, 0f, 0f), Rotation = new Vector3(0f, 0f, 0f), Scale = new Vector3(1f, 1f, 1f) }
            }
        };
        elem11.AddChildElement(elem13);

        var elem14 = new Element
        {
            Name = "RightLazerMesh",
            Position = new DoubleVector3(0d, 0d, -0.081741d, 0d),
            Rotation = new DoubleVector3(-1.570796d, -0d, 0d, 0d),
            Scale = new DoubleVector3(0.05602d, 0.05602d, 0.05602d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "RightLazerMesh", NodeTypeName = "Godot.MeshInstance3D, GodotSharp", Position = new Vector3(0f, 0f, -0.081741f), Rotation = new Vector3(-1.570796f, -0f, 0f), Scale = new Vector3(0.05602f, 0.05602f, 0.05602f) }
            }
        };
        elem13.AddChildElement(elem14);

        var elem15 = new Element
        {
            Name = "RightLazerMesh",
            Position = new DoubleVector3(0d, 0d, -0.081741d, 0d),
            Rotation = new DoubleVector3(-1.570796d, -0d, 0d, 0d),
            Scale = new DoubleVector3(0.05602d, 0.05602d, 0.05602d, 0d),
            Components = new List<IComponent>
            {
                new GenericNodeComponent { Name = "RightLazerMesh", NodeTypeName = "Godot.MeshInstance3D, GodotSharp", Position = new Vector3(0f, 0f, -0.081741f), Rotation = new Vector3(-1.570796f, -0f, 0f), Scale = new Vector3(0.05602f, 0.05602f, 0.05602f) }
            }
        };
        elem14.AddChildElement(elem15);

    }
}
