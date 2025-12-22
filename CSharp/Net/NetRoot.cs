#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Traktor.Core.Net;
using Trakction.XR;
using NVec3 = System.Numerics.Vector3;

public partial class NetRoot : Node
{
    [Export] public int Port = 7777;

    public bool IsServerMode { get; private set; }
    public string RemoteHost { get; private set; } = "127.0.0.1";
    public int PortInUse { get; private set; }
    public string ConnectionStatus { get; private set; } = "Offline";

    private readonly Dictionary<long, int> _playerEntityByPeer = new();

    private const float MaxAllowedPositionMagnitude = 5000.0f;

    private SceneSync? _sceneSync;

    private ServerRepl? _repl;

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
        // if (wantsServer)
        // {
        //     IsServerMode = true;
        //     StartServer(port);

            // Start REPL automatically for headless/dedicated server runs, or when explicitly requested.
            var wantsRepl = args.Contains("--repl");
            var isHeadless = DisplayServer.GetName().ToString().Equals("headless", StringComparison.OrdinalIgnoreCase)
                || OS.HasFeature("dedicated_server");

            if (wantsRepl || isHeadless)
            {
                _repl = new ServerRepl { Name = "ServerRepl" };
                AddChild(_repl);
            }
        // }
        // else if (wantsClient)
        // {
        //     IsServerMode = false;
        //     StartClient(host, port);
        // }
        // else
        // {
        //     IsServerMode = false;
        //     ConnectionStatus = "Offline";
        // }
        if (OS.HasFeature("Server"))
        {
            // Code for headless server mode
            GD.Print("Running in headless server mode.");
            IsServerMode = true;
            StartServer(port);
        }
        else
        {
            // Code for client/GUI mode
            GD.Print("Running in client mode.");
            IsServerMode = false;
            ConnectionStatus = "Offline";
        }

        _sceneSync = GetNodeOrNull<SceneSync>("SceneSync");
        if (_sceneSync == null)
            GD.PushWarning("NetRoot: missing SceneSync child; player spawning will not work.");
    }

    public int ServerSpawnEntity(string packedScenePath, Vector3 position)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ServerSpawnEntity: server-only");
            return 0;
        }

        if (_sceneSync == null)
        {
            GD.PrintErr("ServerSpawnEntity: missing SceneSync");
            return 0;
        }

        return _sceneSync.ServerSpawn(packedScenePath, new Transform3D(Basis.Identity, position));
    }

    public int ServerSpawnBuiltin(string nodeType, Godot.Collections.Dictionary props, Vector3 position)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ServerSpawnBuiltin: server-only");
            return 0;
        }

        if (_sceneSync == null)
        {
            GD.PrintErr("ServerSpawnBuiltin: missing SceneSync");
            return 0;
        }

        return _sceneSync.ServerSpawnBuiltin(nodeType, props, new Transform3D(Basis.Identity, position));
    }

    /// <summary>
    /// Server helper to spawn a simple Box primitive above the server's player entity.
    /// Visible action for a server UI button.
    /// </summary>
    public void ServerSpawnBoxAboveSelf()
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ServerSpawnBoxAboveSelf: server-only");
            return;
        }

        if (_sceneSync == null)
        {
            GD.PrintErr("ServerSpawnBoxAboveSelf: missing SceneSync");
            return;
        }

        var selfId = Multiplayer.GetUniqueId();
        if (!_playerEntityByPeer.TryGetValue(selfId, out var entityId))
        {
            GD.PrintErr("ServerSpawnBoxAboveSelf: no player entity for server peer");
            return;
        }

        if (!_sceneSync.TryGetEntityNode(entityId, out var node))
        {
            GD.PrintErr("ServerSpawnBoxAboveSelf: couldn't find player node");
            return;
        }

        var spawnPos = node.GlobalPosition + new Vector3(0, 2.0f, 0);

        var props = new Godot.Collections.Dictionary
        {
            { "size", new Vector3(0.5f, 0.5f, 0.5f) },
            { "color", new Color(0.2f, 0.6f, 1.0f) }
        };

        ServerSpawnBuiltin("Box", props, spawnPos);
        GD.Print("ServerSpawnBoxAboveSelf: spawned box above server player at " + spawnPos);
    }

    public void ServerDespawnEntity(int entityId)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ServerDespawnEntity: server-only");
            return;
        }

        _sceneSync?.ServerDespawn(entityId);
    }

    public void DebugPrintEntities()
    {
        if (_sceneSync == null)
        {
            GD.Print("No SceneSync.");
            return;
        }

        var ids = _sceneSync.GetEntityIds();
        GD.Print($"Entities: {ids.Length}");
        foreach (var id in ids)
        {
            if (_sceneSync.TryGetEntityNode(id, out var node))
                GD.Print($"  {id}: {node.Name} ({node.SceneFilePath}) pos={node.GlobalPosition}");
        }
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
        _sceneSync?.ClearSpawnedEntities(freeNodes: true);
        
        // If we were a client, also clear the main game scene children so the 3D world is removed on disconnect.
        // Leave the MainGame node itself in place so the UI can re-enter the scene.
        try
        {
            if (!IsServerMode)
            {
                var root = GetTree()?.Root;
                if (root != null)
                {
                    var mainGame = root.GetNodeOrNull<Node>("MainGame");
                    if (mainGame != null)
                    {
                        foreach (var child in mainGame.GetChildren())
                        {
                            if (child is Node n && n.IsInsideTree())
                                n.QueueFree();
                        }
                        GD.Print("NetRoot: cleared MainGame children after disconnect.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"StopNetworking cleanup error: {ex.Message}");
        }
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

        // Send current positions to the joining peer
        foreach (var kv in _playerEntityByPeer)
        {
            var pId = kv.Key;
            if (_sceneSync.TryGetEntityNode(kv.Value, out var node))
            {
                var pos = node.GlobalPosition;
                var rot = node.GlobalRotation;

                if (!IsFinite(pos) || pos.Length() > MaxAllowedPositionMagnitude)
                {
                    GD.PrintErr($"OnPeerConnected: NOT sending invalid pos for peer {pId} to joining {peerId}: {pos}");
                    pos = Vector3.Zero;
                    rot = Vector3.Zero;
                }

                RpcId(peerId, nameof(ClientSetPlayerPosition), pId, pos, rot);
            }
        }

        // Broadcast the new player's bind mapping to everyone.
        Rpc(nameof(ClientBindPlayer), peerId, _playerEntityByPeer[peerId]);
    }

    private void OnPeerDisconnected(long peerId)
    {
        GD.Print($"Peer disconnected: {peerId}");

        if (Multiplayer.IsServer())
        {
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

        // IMPORTANT: peerId is not guaranteed to be small/contiguous (it can be a large random-ish value).
        // Never derive spawn positions from peerId.
        var spawnIndex = _playerEntityByPeer.Count;
        var spawnPos = new Vector3(spawnIndex * 1.5f, 0, 0);
        var entityId = _sceneSync.ServerSpawn("res://assets/Scenes/NetPlayer.tscn", new Transform3D(Basis.Identity, spawnPos));
        _playerEntityByPeer[peerId] = entityId;

        if (_sceneSync.TryGetEntityNode(entityId, out var node) && node is PlayerAvatar avatar)
        {
            avatar.PeerId = peerId;
            avatar.IsLocalPlayer = (Multiplayer.MultiplayerPeer != null && peerId == Multiplayer.GetUniqueId());
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

    // Client -> Server: send local position updates.
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ServerUpdatePlayerPosition(Vector3 position, Vector3 rotation)
    {
        if (!Multiplayer.IsServer())
            return;

        if (!IsFinite(position) || position.Length() > MaxAllowedPositionMagnitude)
        {
            var senderBad = Multiplayer.GetRemoteSenderId();
            GD.PrintErr($"ServerUpdatePlayerPosition: rejecting out-of-range pos from peer {senderBad}: {position}");
            return;
        }

        var sender = Multiplayer.GetRemoteSenderId();
        if (!_playerEntityByPeer.TryGetValue(sender, out var entityId))
            return;

        // Update the server-side node position
        if (_sceneSync.TryGetEntityNode(entityId, out var node))
        {
            node.GlobalPosition = position;
            node.GlobalRotation = rotation;
        }

        // Broadcast to all other clients
        foreach (var peer in Multiplayer.GetPeers())
        {
            if (peer != sender)
                RpcId(peer, nameof(ClientSetPlayerPosition), sender, position, rotation);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ClientSetPlayerPosition(long peerId, Vector3 position, Vector3 rotation)
    {
        if (Multiplayer.GetUniqueId() == peerId)
            return; // ignore for self

        if (!IsFinite(position) || position.Length() > MaxAllowedPositionMagnitude)
        {
            GD.PrintErr($"ClientSetPlayerPosition: ignoring out-of-range pos for peer {peerId}: {position}");
            return;
        }

        if (!_playerEntityByPeer.TryGetValue(peerId, out var entityId))
            return;

        if (_sceneSync.TryGetEntityNode(entityId, out var node))
        {
            node.GlobalPosition = position;
            node.GlobalRotation = rotation;
        }
    }

    public void SendPlayerPosition(Vector3 pos, Vector3 rot)
    {
        if (Multiplayer.MultiplayerPeer == null)
            return;

        // If we're the server (host), don't RPC to ourselves (Godot disallows this by default).
        // Apply locally and broadcast to other peers.
        if (Multiplayer.IsServer())
        {
            var selfId = Multiplayer.GetUniqueId(); // server is always peer 1 in ENet

            if (_sceneSync != null && _playerEntityByPeer.TryGetValue(selfId, out var entityId) && _sceneSync.TryGetEntityNode(entityId, out var node))
            {
                node.GlobalPosition = pos;
                node.GlobalRotation = rot;
            }

            foreach (var peer in Multiplayer.GetPeers())
            {
                if (peer != selfId)
                    RpcId(peer, nameof(ClientSetPlayerPosition), selfId, pos, rot);
            }

            return;
        }

        // Client -> server.
        RpcId(1, nameof(ServerUpdatePlayerPosition), pos, rot);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerStartVr(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;

        GD.Print($"Peer {peerId} started VR mode");

        // Despawn the regular player avatar
        if (_playerEntityByPeer.TryGetValue(peerId, out var oldEntityId))
        {
            _sceneSync?.ServerDespawn(oldEntityId);
            _playerEntityByPeer.Remove(peerId);
        }

        // Spawn the VR player rig
        var spawnIndex = _playerEntityByPeer.Count;
        var spawnPos = new Vector3(spawnIndex * 1.5f, 0, 0);
        var newEntityId = _sceneSync.ServerSpawn("res://assets/Scenes/XrPlayerRig.tscn", new Transform3D(Basis.Identity, spawnPos));
        _playerEntityByPeer[peerId] = newEntityId;

        if (_sceneSync.TryGetEntityNode(newEntityId, out var node) && node is XrPlayerRig rig)
        {
            rig.PeerId = peerId;
            rig.IsLocalPlayer = (Multiplayer.MultiplayerPeer != null && peerId == Multiplayer.GetUniqueId());
        }
    }

    public void StartVr()
    {
        if (Multiplayer.MultiplayerPeer == null)
            return;

        RpcId(1, nameof(ServerStartVr), Multiplayer.GetUniqueId());
    }

    private static bool IsFinite(Vector3 v)
        => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private static NVec3 ToNumerics(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Vector3 ToGodot(NVec3 v) => new(v.X, v.Y, v.Z);
}
