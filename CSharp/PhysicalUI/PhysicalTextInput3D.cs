#nullable enable
using Godot;
using System;
using Trakction.Interaction;

namespace Trakction.PhysicalUI;

/// <summary>
/// Minimal text input stub for physical UI. Full text input usually needs focus management and character events.
/// </summary>
[Tool]
public partial class PhysicalTextInput3D : PhysicalControl3D, IInteractable
{
    [Signal] public delegate void SubmittedEventHandler(string text);

    [Export] public string Text { get; set; } = "";

    public string? InteractionPrompt => Enabled ? "Edit" : null;

    public Action<InteractionContext>? OnActivated { get; set; }
    public Action<InteractionContext, string>? OnSubmitted { get; set; }

    public override void _Ready()
    {
        EnsureDefaultVisuals();
    }

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint())
            EnsureDefaultVisuals();
    }

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
    private void EnsureDefaultVisuals()
    {
        var meshInstance = GetNodeOrNull<MeshInstance3D>("Visuals");
        if (meshInstance == null)
        {
            meshInstance = new MeshInstance3D
            {
                Name = "Visuals",
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.4f, 0.1f, 0.02f)
                }
            };
            AddChild(meshInstance);
        }

        var label = GetNodeOrNull<Label3D>("Label");
        if (label == null)
        {
            label = new Label3D
            {
                Name = "Label",
                PixelSize = 0.001f,
                Position = new Vector3(0, 0, 0.011f), // Slightly in front of mesh
                FontSize = 32,
                OutlineSize = 2
            };
            AddChild(label);
        }
        label.Text = string.IsNullOrEmpty(Text) ? "Input..." : Text;
    }
}
