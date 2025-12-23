#nullable enable
using Godot;
using System;
using Trakction.Interaction;

namespace Trakction.PhysicalUI;

/// <summary>
/// A minimal interactable "button" that can be attached directly to a CSGBox3D.
/// It creates an Area3D + BoxShape3D hit area so InteractorRay3D can raycast it.
/// Visuals are provided by the CSG itself.
/// </summary>
public partial class PhysicalHitBoxButton3D : CsgBox3D, IInteractable
{
    [Signal] public delegate void PressedEventHandler();

    [Export] public bool Enabled { get; set; } = true;

    /// <summary>
    /// If left as Vector3.Zero, the hitbox will default to the current CSG <see cref="CsgBox3D.Size"/>.
    /// </summary>
    [Export] public Vector3 HitBoxSize { get; set; } = Vector3.Zero;

    [Export] public string Prompt { get; set; } = "Press";

    public string? InteractionPrompt => Enabled ? Prompt : null;

    /// <summary>
    /// Optional C# callback (useful for builder/mod wiring).
    /// </summary>
    public Action<InteractionContext>? OnPressed { get; set; }

    public override void _Ready()
    {
        EnsureHitArea();
    }

    public void RefreshHitArea()
    {
        EnsureHitArea();
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

        var size = HitBoxSize;
        if (size == Vector3.Zero)
            size = Size;

        // Ray interaction is easier if the Z thickness isn't paper-thin.
        size.Z = Mathf.Max(size.Z, 0.08f);

        shape.Shape = new BoxShape3D { Size = size };
    }
}
