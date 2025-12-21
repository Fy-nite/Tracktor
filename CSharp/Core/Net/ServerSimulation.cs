using System.Collections.Generic;
using System.Numerics;

namespace Traktor.Core.Net;

public sealed class ServerSimulation
{
    private readonly Dictionary<long, PlayerState> _players = new();

    public float MoveSpeed { get; set; } = 4.5f;

    public IReadOnlyDictionary<long, PlayerState> Players => _players;

    public void EnsurePlayer(long peerId, Vector3 startPosition)
    {
        if (_players.ContainsKey(peerId)) return;
        _players[peerId] = new PlayerState(startPosition, Vector3.Zero, LastProcessedInput: 0);
    }

    public void RemovePlayer(long peerId) => _players.Remove(peerId);

    public PlayerState ApplyInput(long peerId, PlayerInput input)
    {
        if (!_players.TryGetValue(peerId, out var state))
            state = new PlayerState(Vector3.Zero, Vector3.Zero, 0);

        var dir = input.MoveDir;
        if (dir.LengthSquared() > 1e-6f)
            dir = Vector3.Normalize(dir);

        var velocity = dir * MoveSpeed;
        var dt = input.DeltaSeconds;
        if (dt < 0) dt = 0;
        if (dt > 0.1f) dt = 0.1f; // clamp to avoid huge jumps

        var pos = state.Position + velocity * dt;
        state = new PlayerState(pos, velocity, input.Sequence);
        _players[peerId] = state;
        return state;
    }
}
