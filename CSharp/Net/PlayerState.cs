namespace Traktor.Core.Net;

public readonly record struct PlayerState(
    System.Numerics.Vector3 Position,
    System.Numerics.Vector3 Velocity,
    int LastProcessedInput
);
