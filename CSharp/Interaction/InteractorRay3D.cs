#nullable enable
using Godot;

namespace Trakction.Interaction;

/// <summary>
/// Simple ray-based interactor (good default for physical UI / world objects).
/// Add this somewhere on the local player (e.g. camera rig) to enable interaction.
/// </summary>
public partial class InteractorRay3D : Node3D
{
    [Export] public string InteractAction { get; set; } = "ui_accept";
    [Export] public float MaxDistance { get; set; } = 3.0f;

    /// <summary>
    /// If set, we will use this RayCast3D. Otherwise a child RayCast3D named "RayCast3D" is created.
    /// </summary>
    [Export] public NodePath RayCastPath { get; set; } = new();

    private RayCast3D? _ray;

    public override void _Ready()
    {
        SetProcessUnhandledInput(true);

        _ray = !RayCastPath.IsEmpty ? GetNodeOrNull<RayCast3D>(RayCastPath) : GetNodeOrNull<RayCast3D>("RayCast3D");
        if (_ray == null)
        {
            _ray = new RayCast3D { Name = "RayCast3D" };
            AddChild(_ray);
        }

        _ray.Enabled = true;
        _ray.CollideWithAreas = true;
        _ray.CollideWithBodies = true;
        _ray.TargetPosition = new Vector3(0, 0, -MaxDistance);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey and not InputEventMouseButton and not InputEventJoypadButton)
            return;

        if (!Input.IsActionJustPressed(InteractAction))
            return;

        TryInteract(@event);
    }

    /// <summary>
    /// Attempts to interact with the first <see cref="IInteractable"/> along the ray.
    /// This is callable from XR controller button events (e.g. trigger press).
    /// </summary>
    public bool TryInteract(InputEvent? sourceEvent = null)
    {

        if (_ray == null)
            return false;

        _ray.TargetPosition = new Vector3(0, 0, -MaxDistance);
        _ray.ForceRaycastUpdate();

        if (!_ray.IsColliding())
            return false;

        var collider = _ray.GetCollider();
        if (collider is not Node hitNode)
            return false;

        var interactable = FindInteractable(hitNode);
        if (interactable == null)
            return false;

        var ctx = new InteractionContext(
            Interactor: this,
            HitNode: hitNode,
            HitPosition: _ray.GetCollisionPoint(),
            HitNormal: _ray.GetCollisionNormal(),
            SourceEvent: sourceEvent
        );

        if (!interactable.CanInteract(in ctx))
            return false;

        interactable.Interact(in ctx);

        return true;
    }

    private static IInteractable? FindInteractable(Node start)
    {
        Node? n = start;
        while (n != null)
        {
            if (n is IInteractable i)
                return i;
            n = n.GetParent();
        }
        return null;
    }
}
