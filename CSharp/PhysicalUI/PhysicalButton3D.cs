#nullable enable
using Godot;
using System;
using Trakction.Interaction;

namespace Trakction.PhysicalUI;

[Tool]
public partial class PhysicalButton3D : PhysicalControl3D, IInteractable
{
    [Signal] public delegate void PressedEventHandler();

    [Export] public string Text { get; set; } = "Button";
    [Export] public Vector3 HitBoxSize { get; set; } = new(0.35f, 0.12f, 0.12f);

    public string? InteractionPrompt => Enabled ? "Press" : null;

    /// <summary>
    /// Optional C# callback (useful for builder/mod wiring).
    /// </summary>
    public Action<InteractionContext>? OnPressed { get; set; }

    public override void _Ready()
    {
        EnsureDefaultVisuals();
        EnsureHitArea();
    }

    public override void _EnterTree()
    {
        // Ensure visuals are present while editing the scene in the editor.
        if (Engine.IsEditorHint())
        {
            EnsureDefaultVisuals();
            EnsureHitArea();
        }
    }

    public bool CanInteract(in InteractionContext context) => Enabled;

    public void Interact(in InteractionContext context)
    {
        if (!Enabled)
            return;

        EmitSignal(SignalName.Pressed);
        OnPressed?.Invoke(context);
    }

    private void EnsureHitArea()
    {
        var area = GetNodeOrNull<Area3D>("HitArea");
        if (area == null)
        {
            area = new Area3D { Name = "HitArea" };
            AddChild(area);
        }

        var shape = area.GetNodeOrNull<CollisionShape3D>("Shape");
        if (shape == null)
        {
            shape = new CollisionShape3D { Name = "Shape" };
            area.AddChild(shape);
        }

        shape.Shape = new BoxShape3D { Size = HitBoxSize };
    }

    private void EnsureDefaultVisuals()
    {
        // Keep visuals extremely minimal so this stays "builder-friendly".
        // Mods can replace these with their own meshes/materials.
        var mesh = GetNodeOrNull<MeshInstance3D>("Mesh");
        if (mesh == null)
        {
            mesh = new MeshInstance3D { Name = "Mesh" };
            mesh.Mesh = new BoxMesh { Size = new Vector3(0.35f, 0.12f, 0.12f) };
            AddChild(mesh);
        }

        var label = GetNodeOrNull<Label3D>("Label");
        if (label == null)
        {
            label = new Label3D { Name = "Label" };
            label.Position = new Vector3(0f, 0f, 0.1f); 
            label.PixelSize = 0.0005f;
            label.OutlineSize = 5;
            // make the size large enough so that it looks clean.
            label.FontSize = 64;
            
            AddChild(label);
        }

        label.Text = Text;
        
        // label.Scale = new Vector3(4f, 4f, 4f); // Scale up so it's readable?
        // calculate the size of the label based on the text length
        label.Scale = new Vector3(1f * Text.Length, 1f * Text.Length, 1f * Text.Length);
    }
}
