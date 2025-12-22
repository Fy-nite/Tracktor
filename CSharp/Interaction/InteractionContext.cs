#nullable enable
using Godot;

namespace Trakction.Interaction;

public readonly record struct InteractionContext(
    Node Interactor,
    Node? HitNode,
    Vector3 HitPosition,
    Vector3 HitNormal,
    InputEvent? SourceEvent
);
