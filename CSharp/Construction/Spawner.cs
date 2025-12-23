#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Traktion.CSharp.Construction;

public partial class Spawner : Node3D
{
    // Signal emitted after spawn: (instance, packedScenePath)
    [Signal]
    public delegate void SpawnedEventHandler(Node3D? instance, string? scenePath);

    // Inspector-exposed list of spawnable PackedScenes
    [Export]
    public PackedScene[] SpawnableScenes { get; set; } = Array.Empty<PackedScene>();

    // Optional parent path for spawned objects. If empty, uses provided parent or scene root.
    [Export]
    public NodePath SpawnParentPath { get; set; } = new NodePath();

    // Default spawn offset relative to this node's global position
    [Export]
    public Vector3 SpawnOffset { get; set; } = new Vector3(0, 1, 0);

    // Optional rotation randomization
    [Export]
    public bool RandomizeRotation { get; set; }
    [Export]
    public Vector3 RotationJitter { get; set; } = new Vector3(0, 180, 0); // degrees

    public int Count => SpawnableScenes.Length;

    public override void _Ready()
    {
        // Nothing required by default. UI scenes will call GetSpawnableNames() and Spawn(...)
    }

    // Return friendly names for UI lists (scene filename without extension)
    public string[] GetSpawnableNames()
    {
        var names = new List<string>();
        foreach (var ps in SpawnableScenes)
        {
            var path = ps.ResourcePath;
            var name = string.IsNullOrEmpty(path) ? ps.ToString() : Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(name))
                name = ps.ToString();
            names.Add(name);
        }
        return names.ToArray();
    }

    // Add/remove runtime helpers
    public void AddSpawnable(PackedScene scene)
    {
        var list = new List<PackedScene>(SpawnableScenes);
        list.Add(scene);
        SpawnableScenes = list.ToArray();
    }

    public bool RemoveSpawnableAt(int index)
    {
        if (index < 0 || index >= Count) return false;
        var list = new List<PackedScene>(SpawnableScenes);
        list.RemoveAt(index);
        SpawnableScenes = list.ToArray();
        return true;
    }

    // Main spawn method
    public Node3D? Spawn(int index, Vector3? position = null, Node? parent = null)
    {
        if (index < 0 || index >= Count)
        {
            GD.PrintErr($"Spawner.Spawn: index {index} out of range (0..{Math.Max(0, Count - 1)})");
            return null;
        }

        var packed = SpawnableScenes[index];

        var instObj = packed.Instantiate();
        if (instObj == null)
        {
            GD.PrintErr($"Spawner.Spawn: failed to instantiate scene at index {index}");
            return null;
        }

        // Determine parent
        Node spawnParent = parent ?? (GetNodeOrNull<Node>(SpawnParentPath) ?? GetTree().CurrentScene ?? GetTree().Root);

        // Positioning
        Vector3 spawnPosition = position ?? GlobalPosition + SpawnOffset;
        if (instObj is Node3D inst3)
        {
            inst3.GlobalPosition = spawnPosition;
            if (RandomizeRotation)
            {
                var r = new Vector3(
                    (GD.Randf() * 2 - 1) * RotationJitter.X,
                    (GD.Randf() * 2 - 1) * RotationJitter.Y,
                    (GD.Randf() * 2 - 1) * RotationJitter.Z
                );
                inst3.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(r.X));
                inst3.RotateObjectLocal(Vector3.Up, Mathf.DegToRad(r.Y));
                inst3.RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(r.Z));
            }
        }

        spawnParent.AddChild(instObj);

        // Emit Spawned signal with the instance and the PackedScene.ResourcePath (non-null string)
        var path = packed?.ResourcePath ?? string.Empty;
        EmitSignal(SignalName.Spawned, instObj as Node3D, path);
        return instObj as Node3D;
    }

    // Convenience spawn by name (first match)
    public Node3D? SpawnByName(string name, Vector3? position = null, Node? parent = null)
    {
        var names = GetSpawnableNames();
        for (int i = 0; i < names.Length; i++)
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                return Spawn(i, position, parent);
        return null;
    }

    // === Primitive spawning helpers ===
    public enum PrimitiveKind
    {
        Box = 0,
        Sphere = 1,
        Cylinder = 2,
        Plane = 3,
        Capsule = 4
    }

    // Names for the supported primitives (useful for building UI lists)
    public string[] GetPrimitiveNames() => new[] { "Box", "Sphere", "Cylinder", "Plane", "Capsule" };

    // Spawn a simple primitive (MeshInstance3D with a built-in primitive mesh)
    public Node3D? SpawnPrimitive(PrimitiveKind kind, Vector3? position = null, Node? parent = null)
    {
        Node spawnParent = parent ?? (GetNodeOrNull<Node>(SpawnParentPath) ?? GetTree().CurrentScene ?? GetTree().Root);
        Vector3 spawnPosition = position ?? GlobalPosition + SpawnOffset;

        var meshInst = new MeshInstance3D();
        switch (kind)
        {
            case PrimitiveKind.Box:
                meshInst.Mesh = new BoxMesh { Size = new Vector3(1, 1, 1) };
                break;
            case PrimitiveKind.Sphere:
                meshInst.Mesh = new SphereMesh { Radius = 0.5f };
                break;
            case PrimitiveKind.Cylinder:
                var cyl = new CylinderMesh();
                cyl.Height = 1f;
                meshInst.Mesh = cyl;
                break;
            case PrimitiveKind.Plane:
                meshInst.Mesh = new PlaneMesh { Size = new Vector2(1, 1) };
                break;
            case PrimitiveKind.Capsule:
                var cap = new CapsuleMesh();
                cap.Height = 0.8f;
                meshInst.Mesh = cap;
                break;
            default:
                GD.PrintErr($"SpawnPrimitive: unsupported primitive kind {kind}");
                return null;
        }

        meshInst.GlobalPosition = spawnPosition;
        spawnParent.AddChild(meshInst);
        // Emit Spawned with empty scenePath for primitives
        EmitSignal(SignalName.Spawned, meshInst, string.Empty);
        return meshInst;
    }

    public Node3D? SpawnPrimitiveByName(string name, Vector3? position = null, Node? parent = null)
    {
        var primitives = GetPrimitiveNames();
        for (int i = 0; i < primitives.Length; i++)
            if (string.Equals(primitives[i], name, StringComparison.OrdinalIgnoreCase))
                return SpawnPrimitive((PrimitiveKind)i, position, parent);
        return null;
    }

}
