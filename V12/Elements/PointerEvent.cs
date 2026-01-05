using Godot;
using V12.Interfaces;

namespace V12.Elements;

public class PointerEvent : IEvent
{
    public enum Kind { Entered, Exited, Pressed, Released, Moved }

    public Kind EventKind { get; }
    public Node3D Pointer { get; }
    public Node3D? Target { get; }
    public Vector3 Position { get; }
    public Vector3 LastPosition { get; }

    public PointerEvent(Kind kind, Node3D pointer, Node3D? target, Vector3 position, Vector3 lastPosition)
    {
        EventKind = kind;
        Pointer = pointer;
        Target = target;
        Position = position;
        LastPosition = lastPosition;
    }

    public void fireEvent(IEventCallback callback)
    {
        callback.callback?.Invoke(this);
    }
}
