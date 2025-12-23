#nullable enable
using Godot;
using System;
using Trakction.Interaction;
using Trakction.PhysicalUI;


namespace Traktion.CSharp.Construction;

public partial class SpawnerPanelBuilder : Node3D
{
    [Export] public NodePath SpawnerPath { get; set; } = new NodePath();

    [Export]
    public NodePath ScrollPanelPath { get; set; } = new NodePath("ScrollPanel");

    // Optional PackedScene template for buttons; if not provided we instantiate PhysicalButton3D directly
    [Export]
    public PackedScene? ButtonTemplate { get; set; }

    // Optional camera wiring: if set, buttons will spawn objects in front of this camera
    [Export]
    public NodePath CameraPath { get; set; } = new NodePath();

    [Export]
    public float SpawnDistance { get; set; } = 2.0f;

    [Export]
    public Vector3 SpawnOffset { get; set; } = Vector3.Zero;

    private Spawner? _spawner;
    private PhysicalScrollBox3D? _scroll;
    private Camera3D? _camera;

    public override void _Ready()
    {
        _spawner = GetNodeOrNull<Spawner>(SpawnerPath) ?? GetParent()?.GetNodeOrNull<Spawner>(SpawnerPath);
        _scroll = GetNodeOrNull<PhysicalScrollBox3D>(ScrollPanelPath);

        // Resolve camera if a path was provided
        if (CameraPath != new NodePath())
        {
            var node = GetNodeOrNull(CameraPath);
            if (node is Camera3D cam)
                _camera = cam;
            else if (node is Node3D nd)
                _camera = nd as Camera3D; // null if not a camera
        }

        // If spawner not found via path, try to locate any Spawner in the current scene tree
        if (_spawner == null)
        {
            var root = GetTree().CurrentScene;
            if (root != null)
                _spawner = FindSpawnerInTree(root);
        }
        
        if (_spawner == null)
        {
            GD.PrintErr($"SpawnerPanelBuilder: could not find Spawner at path '{SpawnerPath}' or in the scene tree.");
            return;
        }
        if (_scroll == null)
        {
            GD.PrintErr($"SpawnerPanelBuilder: could not find PhysicalScrollBox3D at path '{ScrollPanelPath}'");
            return;
        }

        BuildButtons();
    }

    private void BuildButtons()
    {
        var spawner = _spawner!; // non-null after checks in _Ready
        var scroll = _scroll!;

        // Utility button: open the (temporary) 2D inspector overlay.
        {
            var inspectorBtn = CreateButton("Inspector");
            inspectorBtn.OnPressed = (InteractionContext ctx) => InspectorPanelService.Toggle(this);
            scroll.AddChild(inspectorBtn);
            if (inspectorBtn is Node3D i3d)
                i3d.Position = new Vector3(0, 0, 0);
        }

        // Create buttons for PackedScene spawnables
        var names = spawner.GetSpawnableNames();
        float yOffset = 0.2f;
        const float buttonSpacing = 0.2f; // Adjust as needed for spacing between buttons
        for (int i = 0; i < names.Length; i++)
        {
            var btn = CreateButton(names[i]);
            int idx = i; // capture
            btn.OnPressed = (InteractionContext ctx) => spawner.Spawn(idx, ComputeSpawnPosition(in ctx));
            scroll.AddChild(btn);
            // Manually position buttons vertically (assuming scroll is vertical)
            if (btn is Node3D btn3d)
            {
                btn3d.Position = new Vector3(0, -yOffset, 0);
                yOffset += buttonSpacing;
            }
        }

        // Add a small separator (non-interactive label) - reuse a button but disable it
        var sep = CreateButton("-- Primitives --");
        sep.Enabled = false;
        scroll.AddChild(sep);
        if (sep is Node3D sep3d)
        {
            sep3d.Position = new Vector3(0, -yOffset, 0);
            yOffset += buttonSpacing;
        }

        // Add primitive buttons
        var primNames = spawner.GetPrimitiveNames();
        for (int j = 0; j < primNames.Length; j++)
        {
            var pbtn = CreateButton(primNames[j]);
            int pidx = j;
            pbtn.OnPressed = (InteractionContext ctx) => spawner.SpawnPrimitive((Spawner.PrimitiveKind)pidx, ComputeSpawnPosition(in ctx));
            scroll.AddChild(pbtn);
            if (pbtn is Node3D pbtn3d)
            {
                pbtn3d.Position = new Vector3(0, -yOffset, 0);
                yOffset += buttonSpacing;
            }
        }
    }

    private Vector3? ComputeSpawnPosition()
    {
        if (_camera == null)
            return null;

        var camTransform = _camera.GlobalTransform;
        var forward = -camTransform.Basis.Z;
        return camTransform.Origin + forward * SpawnDistance + SpawnOffset;
    }

    private Vector3? ComputeSpawnPosition(in InteractionContext ctx)
    {
        // Prefer the actual interactor node (works for XR hands/controllers and desktop interactors).
        if (ctx.Interactor is Node3D interactor3d)
        {
            var t = interactor3d.GlobalTransform;
            var forward = -t.Basis.Z;
            return t.Origin + forward * SpawnDistance + SpawnOffset;
        }

        // Fallback to camera if available.
        return ComputeSpawnPosition();
    }

    private PhysicalButton3D CreateButton(string text)
    {
        PhysicalButton3D? btn = null;
        if (ButtonTemplate != null)
        {
            var inst = ButtonTemplate.Instantiate();
            btn = inst as PhysicalButton3D ?? inst.GetChildOrNull<PhysicalButton3D>(0);
            if (btn == null)
            {
                GD.PrintErr("SpawnerPanelBuilder: ButtonTemplate does not produce a PhysicalButton3D. Falling back to direct construction.");
            }
        }

        if (btn == null)
            btn = new PhysicalButton3D();

        btn.Text = text;
        return btn;
    }

    // Recursive search for a Spawner in the scene tree. Stops at first match.
    private Spawner? FindSpawnerInTree(Node? root)
    {
        if (root == null)
            return null;

        if (root is Spawner spawner)
            return spawner;

        foreach (var childObj in root.GetChildren())
        {
            if (childObj is Node child)
            {
                var found = FindSpawnerInTree(child);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}
