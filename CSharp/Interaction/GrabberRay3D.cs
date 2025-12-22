#nullable enable
using Godot;

namespace Trakction.Interaction;

/// <summary>
/// Grip-to-grab ray grabber.
/// - If the hit node (or a parent) implements IGrabbable, delegates to it.
/// - Otherwise, does a basic grab for Node3D that is in the "grabbable" group.
/// </summary>
public partial class GrabberRay3D : Node3D
{
    [Export] public string GrabAction { get; set; } = "grab";
    [Export] public float MaxDistance { get; set; } = 3.0f;

    /// <summary>
    /// How far in front of the grabber to hold grabbed objects (in meters).
    /// </summary>
    [Export] public float HoldDistance { get; set; } = 0.25f;

    /// <summary>
    /// If set, we will use this RayCast3D. Otherwise a child RayCast3D named "RayCast3D" is created.
    /// </summary>
    [Export] public NodePath RayCastPath { get; set; } = new();

    private RayCast3D? _ray;
    private Node3D? _holdPoint;

    private Node3D? _grabbed;
    private Node? _originalParent;
    private RigidBody3D? _grabbedBody;

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

        _holdPoint = GetNodeOrNull<Node3D>("HoldPoint");
        if (_holdPoint == null)
        {
            _holdPoint = new Node3D { Name = "HoldPoint" };
            AddChild(_holdPoint);
        }

        _holdPoint.Position = new Vector3(0, 0, -HoldDistance);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey and not InputEventMouseButton and not InputEventJoypadButton)
            return;

        if (Input.IsActionJustPressed(GrabAction))
            TryGrab();

        if (Input.IsActionJustReleased(GrabAction))
            Release();
    }

    /// <summary>
    /// XR can call this on grip press.
    /// </summary>
    public bool TryGrab()
    {
        if (_grabbed != null)
            return true;

        if (_ray == null || _holdPoint == null)
            return false;

        _ray.TargetPosition = new Vector3(0, 0, -MaxDistance);
        _ray.ForceRaycastUpdate();

        if (!_ray.IsColliding())
            return false;

        var collider = _ray.GetCollider();
        if (collider is not Node hitNode)
            return false;

        var grabbable = FindGrabbable(hitNode);
        var ctx = new GrabContext(
            Grabber: this,
            HitNode: hitNode,
            HitPosition: _ray.GetCollisionPoint(),
            HitNormal: _ray.GetCollisionNormal()
        );

        if (grabbable != null)
        {
            if (!grabbable.CanGrab(in ctx))
                return false;

            grabbable.Grab(in ctx);
            return true;
        }

        // Basic fallback grab: opt-in via group.
        var node3d = FindNode3D(hitNode);
        if (node3d == null)
            return false;

        if (!node3d.IsInGroup("grabbable"))
            return false;

        BasicGrab(node3d);
        return true;
    }

    /// <summary>
    /// XR can call this on grip release.
    /// </summary>
    public void Release()
    {
        if (_grabbed == null)
            return;

        var grabbedNow = _grabbed;
        var ctx = new GrabContext(
            Grabber: this,
            HitNode: grabbedNow,
            HitPosition: grabbedNow.GlobalPosition,
            HitNormal: Vector3.Zero
        );

        // If the grabbed object implements IGrabbable, let it handle release.
        if (grabbedNow is IGrabbable g)
        {
            g.Release(in ctx);
            _grabbed = null;
            _originalParent = null;
            _grabbedBody = null;
            return;
        }

        BasicRelease();
    }

    private void BasicGrab(Node3D target)
    {
        if (_holdPoint == null)
            return;

        _grabbed = target;
        _originalParent = target.GetParent();
        _grabbedBody = target as RigidBody3D;

        var global = target.GlobalTransform;

        // Move under hold point so it follows the hand.
        if (_originalParent != null)
            _originalParent.RemoveChild(target);

        _holdPoint.AddChild(target);
        target.GlobalTransform = global;

        if (_grabbedBody != null)
        {
            _grabbedBody.LinearVelocity = Vector3.Zero;
            _grabbedBody.AngularVelocity = Vector3.Zero;
            _grabbedBody.Freeze = true;
        }
    }

    private void BasicRelease()
    {
        if (_grabbed == null)
            return;

        var target = _grabbed;
        var global = target.GlobalTransform;

        // Detach from hold point.
        target.GetParent()?.RemoveChild(target);

        // Restore to original parent if it still exists.
        if (_originalParent != null && GodotObject.IsInstanceValid(_originalParent))
        {
            _originalParent.AddChild(target);
        }
        else
        {
            // Fallback: put back into the current scene root.
            GetTree().CurrentScene?.AddChild(target);
        }

        target.GlobalTransform = global;

        if (_grabbedBody != null)
            _grabbedBody.Freeze = false;

        _grabbed = null;
        _originalParent = null;
        _grabbedBody = null;
    }

    private static IGrabbable? FindGrabbable(Node start)
    {
        Node? n = start;
        while (n != null)
        {
            if (n is IGrabbable g)
                return g;
            n = n.GetParent();
        }
        return null;
    }

    private static Node3D? FindNode3D(Node start)
    {
        Node? n = start;
        while (n != null)
        {
            if (n is Node3D n3d)
                return n3d;
            n = n.GetParent();
        }
        return null;
    }
}
