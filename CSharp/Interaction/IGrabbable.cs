#nullable enable
using Godot;

namespace Trakction.Interaction;

/// <summary>
/// Optional opt-in interface for objects that support grip-to-grab behavior.
/// If not implemented, GrabberRay3D can still do a basic grab for Node3D in the "grabbable" group.
/// </summary>
public interface IGrabbable
{
    bool CanGrab(in GrabContext context);

    /// <summary>
    /// Called when the interactor begins grabbing.
    /// Implementations can manage reparenting/physics however they like.
    /// </summary>
    void Grab(in GrabContext context);

    /// <summary>
    /// Called when the interactor releases the grab.
    /// </summary>
    void Release(in GrabContext context);
}
