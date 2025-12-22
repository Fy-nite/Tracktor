#nullable enable
using Godot;

namespace Trakction.Interaction;

public interface IInteractable
{
    /// <summary>
    /// Optional short prompt for UI/hover hints (e.g. "Press E" / "Use").
    /// </summary>
    string? InteractionPrompt { get; }

    bool CanInteract(in InteractionContext context);
    void Interact(in InteractionContext context);
}
