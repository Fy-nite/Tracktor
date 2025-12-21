#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Traktor.Core.Net;
using NVec3 = System.Numerics.Vector3;

public partial class NetRoot : Node
{
    [Export] public int Port = 7777;

    public bool IsServerMode { get; private set; }
    public string RemoteHost { get; private set; } = "127.0.0.1";
    public int PortInUse { get; private set; }
    public string ConnectionStatus { get; private set; } = "Offline";

    private readonly Dictionary<long, int> _playerEntityByPeer = new();
    private readonly ServerSimulation _serverSim = new();

    private SceneSync? _sceneSync;

    public override void _Ready()
    {
        AddToGroup("net_root");

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += () =>
        {
            ConnectionStatus = "Connected";
            GD.Print("Connected to server");
        };
        Multiplayer.ConnectionFailed += () =>
        {
            ConnectionStatus = "ConnectionFailed";
            GD.PrintErr("Connection failed");
        };

        var args = OS.GetCmdlineArgs().ToList();

        var wantsServer = args.Contains("--server");
        var connectIndex = args.IndexOf("--connect");
        var wantsClient = connectIndex >= 0;

        var host = "127.0.0.1";
        var port = Port;

        if (wantsClient && connectIndex + 1 < args.Count)
            host = args[connectIndex + 1];

        var portIndex = args.IndexOf("--port");
        if (portIndex >= 0 && portIndex + 1 < args.Count && int.TryParse(args[portIndex + 1], out var parsedPort))
            port = parsedPort;

        RemoteHost = host;
        PortInUse = port;

        // Important: Only autostart when explicitly requested.
        if (wantsServer)
        {
            IsServerMode = true;
            StartServer(port);
        }
        else if (wantsClient)
        {
            IsServerMode = false;
            StartClient(host, port);
        }
        else
        {
            IsServerMode = false;
            ConnectionStatus = "Offline";
        }

        _sceneSync = GetNodeOrNull<SceneSync>("SceneSync");
        if (_sceneSync == null)
            GD.PushWarning("NetRoot: missing SceneSync child; player spawning will not work.");
    }

    public void StartServerFromUi(int port)
    {
        StopNetworking();
        IsServerMode = true;
        PortInUse = port;
        StartServer(port);
    }

    public void StartClientFromUi(string host, int port)
    {
        StopNetworking();
        IsServerMode = false;
        RemoteHost = host;
        PortInUse = port;
        StartClient(host, port);
    }

    public void StopNetworking()
    {
        try
        {
            if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enet)
                enet.Close();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"StopNetworking error: {ex.Message}");
        }

        Multiplayer.MultiplayerPeer = null;
        ConnectionStatus = "Offline";

        _playerEntityByPeer.Clear();
        _serverSim.Players.ToString(); // no-op to keep sim referenced
        _sceneSync?.ClearSpawnedEntities(freeNodes: true);
    }

    private void StartServer(int port)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port, maxClients: 32);
        Multiplayer.MultiplayerPeer = peer;
        ConnectionStatus = "Listening";
        GD.Print($"Server listening on {port}");

        // Spawn server's own player (peerId=1) so you can test in non-headless too.
        if (_sceneSync != null)
            ServerEnsurePlayer(1);
    }

    private void StartClient(string host, int port)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(host, port);
        Multiplayer.MultiplayerPeer = peer;
        ConnectionStatus = "Connecting";
        GD.Print($"Client connecting to {host}:{port}");
    }

    private void OnPeerConnected(long peerId)
    {
        GD.Print($"Peer connected: {peerId}");

        if (!Multiplayer.IsServer())
            return;

        if (_sceneSync == null)
            return;

        // Ensure server-side player entity exists.
        ServerEnsurePlayer(peerId);

        // Tell the joining peer how to bind existing players to entity ids.
        foreach (var kv in _playerEntityByPeer)
            RpcId(peerId, nameof(ClientBindPlayer), kv.Key, kv.Value);

        // Broadcast the new player's bind mapping to everyone.
        Rpc(nameof(ClientBindPlayer), peerId, _playerEntityByPeer[peerId]);
    }

    private void OnPeerDisconnected(long peerId)
    {
        GD.Print($"Peer disconnected: {peerId}");

        if (Multiplayer.IsServer())
        {
            _serverSim.RemovePlayer(peerId);

            if (_sceneSync != null && _playerEntityByPeer.TryGetValue(peerId, out var entityId))
                _sceneSync.ServerDespawn(entityId);

            _playerEntityByPeer.Remove(peerId);
            Rpc(nameof(ClientUnbindPlayer), peerId);
        }
    }

    private void ServerEnsurePlayer(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;
        if (_sceneSync == null)
            return;
        if (_playerEntityByPeer.ContainsKey(peerId))
            return;

        var spawnPos = new Vector3((peerId - 1) * 1.5f, 0, 0);
        var entityId = _sceneSync.ServerSpawn("res://assets/Scenes/NetPlayer.tscn", new Transform3D(Basis.Identity, spawnPos));
        _playerEntityByPeer[peerId] = entityId;
        _serverSim.EnsurePlayer(peerId, ToNumerics(spawnPos));

        if (_sceneSync.TryGetEntityNode(entityId, out var node) && node is PlayerAvatar avatar)
        {
            avatar.PeerId = peerId;
            avatar.IsLocalPlayer = false;
            avatar.Name = $"Player_{peerId}";
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientBindPlayer(long peerId, int entityId)
    {
        _playerEntityByPeer[peerId] = entityId;

        if (_sceneSync == null)
            return;
        if (!_sceneSync.TryGetEntityNode(entityId, out var node))
            return;
        if (node is not PlayerAvatar avatar)
            return;

        avatar.PeerId = peerId;
        avatar.IsLocalPlayer = (Multiplayer.MultiplayerPeer != null && peerId == Multiplayer.GetUniqueId());
        avatar.Name = $"Player_{peerId}";
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientUnbindPlayer(long peerId)
    {
        _playerEntityByPeer.Remove(peerId);
    }

    // Client -> Server: send local input (local prediction is done client-side).
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ServerSubmitInput(int sequence, float deltaSeconds, Vector3 moveDir)
    {
        if (!Multiplayer.IsServer())
            return;

        if (_sceneSync == null)
            return;

        var sender = Multiplayer.GetRemoteSenderId();
        ServerEnsurePlayer(sender);

        if (!_playerEntityByPeer.TryGetValue(sender, out var entityId))
            return;
        if (!_sceneSync.TryGetEntityNode(entityId, out var node) || node is not PlayerAvatar avatar)
            return;

        var input = new PlayerInput(sequence, deltaSeconds, ToNumerics(moveDir));
        var newState = _serverSim.ApplyInput(sender, input);

        // Move the authoritative server-side player node. SceneSync will replicate its transform.
        avatar.GlobalPosition = ToGodot(newState.Position);
    }

    public void SubmitLocalInput(int sequence, float deltaSeconds, Vector3 moveDir)
    {
        // Server is always peer 1 in Godot ENet.
        RpcId(1, nameof(ServerSubmitInput), sequence, deltaSeconds, moveDir);
    }

    private static NVec3 ToNumerics(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Vector3 ToGodot(NVec3 v) => new(v.X, v.Y, v.Z);
}
