namespace Traktor.Core.Net;

public readonly record struct PlayerInput(
    int Sequence,
    float DeltaSeconds,
    System.Numerics.Vector3 MoveDir
);
