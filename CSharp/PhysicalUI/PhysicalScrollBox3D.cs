#nullable enable
using Godot;
using System;
using Trakction.Interaction;

namespace Trakction.PhysicalUI;

/// <summary>
/// Placeholder "physical" scroll container. Real implementations typically handle pointer hover + wheel/drag.
/// This class exists so mods can target a stable API now.
/// </summary>
public partial class PhysicalScrollBox3D : PhysicalControl3D, IInteractable
{
    [Signal] public delegate void ScrolledEventHandler(float delta);

    [Export] public float ScrollValue { get; set; }
    [Export] public float ScrollStep { get; set; } = 1.0f;

    public string? InteractionPrompt => Enabled ? "Scroll" : null;

    public Action<InteractionContext, float>? OnScrolled { get; set; }

    public bool CanInteract(in InteractionContext context) => Enabled;

    public void Interact(in InteractionContext context)
    {
        // Minimal: treat a generic "interact" press as one scroll step.
        if (!Enabled)
            return;

        ApplyScroll(ScrollStep, in context);
    }

    public void ApplyScroll(float delta, in InteractionContext context)
    {
        if (!Enabled)
            return;

        ScrollValue += delta;
        EmitSignal(SignalName.Scrolled, delta);
        OnScrolled?.Invoke(context, delta);
    }
}
