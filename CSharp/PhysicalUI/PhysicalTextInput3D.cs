#nullable enable
using Godot;
using System;
using Trakction.Interaction;

namespace Trakction.PhysicalUI;

/// <summary>
/// Minimal text input stub for physical UI. Full text input usually needs focus management and character events.
/// </summary>
public partial class PhysicalTextInput3D : PhysicalControl3D, IInteractable
{
    [Signal] public delegate void SubmittedEventHandler(string text);

    [Export] public string Text { get; set; } = "";

    public string? InteractionPrompt => Enabled ? "Edit" : null;

    public Action<InteractionContext>? OnActivated { get; set; }
    public Action<InteractionContext, string>? OnSubmitted { get; set; }

    public bool CanInteract(in InteractionContext context) => Enabled;

    public void Interact(in InteractionContext context)
    {
        // Minimal behavior: activation hook (mods can open their own keyboard, etc.).
        if (!Enabled)
            return;

        OnActivated?.Invoke(context);
    }

    public void Submit(in InteractionContext context)
    {
        if (!Enabled)
            return;

        EmitSignal(SignalName.Submitted, Text);
        OnSubmitted?.Invoke(context, Text);
    }
}
