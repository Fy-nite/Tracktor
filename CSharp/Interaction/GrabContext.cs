#nullable enable
using Godot;

namespace Trakction.Interaction;

public readonly record struct GrabContext(
    Node Grabber,
    Node? HitNode,
    Vector3 HitPosition,
    Vector3 HitNormal
);
