#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using Trakction.XR;

/// <summary>
/// Minimal, server-authoritative scene syncing for Node3D instances.
///
/// - Server spawns/despawns entities and sends reliable RPCs.
/// - Server streams transform deltas (unreliable) for registered entities.
/// - Clients never authoritatively move synced entities.
///
/// Attach this under a node that exists on both server + client (e.g. under NetRoot).
/// Set SyncRootPath to the node that will parent all synced entities.
///
/// Usage:
/// - Server: call ServerSpawn("res://path/to/MyThing.tscn", transform)
/// - Nodes spawned this way get an entity id and will replicate to all clients.
/// </summary>
public partial class SceneSync : Node
{
    [Export] public NodePath? SyncRootPath;
    [Export] public float TransformSendHz = 15.0f;

    // For nodes that already exist in the scene on both server and client.
    // Add Node3D nodes to this group to have their transforms synced by relative NodePath.
    [Export] public string PathSyncGroup = "net_sync";

    private Node _syncRoot = null!;

    private int _nextEntityId = 1;
    private readonly Dictionary<int, Node3D> _entityById = new();
    private readonly Dictionary<Node3D, int> _idByEntity = new();
    private readonly Dictionary<int, Transform3D> _lastSentTransform = new();

    private readonly Dictionary<string, Node3D> _pathEntityByRelPath = new();
    private readonly Dictionary<string, Transform3D> _lastSentPathTransform = new();

    private double _sendAccumulator;

    public bool TryGetEntityNode(int entityId, out Node3D node)
    {
        if (_entityById.TryGetValue(entityId, out var found) && IsInstanceValid(found))
        {
            node = found;
            return true;
        }

        node = null!;
        return false;
    }

    private Vector3? GetVector3FromProps(Godot.Collections.Dictionary props, string key)
    {
        if (props == null)
            return null;

        try
        {
            if (props.ContainsKey(key))
            {
                var val = props[key];
                try
                {
                    return (Vector3)val;
                }
                catch { }

                try
                {
                    var arr = (Godot.Collections.Array)val;
                    if (arr != null && arr.Count >= 3)
                        return new Vector3(Convert.ToSingle(arr[0]), Convert.ToSingle(arr[1]), Convert.ToSingle(arr[2]));
                }
                catch { }
            }

            if (props.ContainsKey(key + "_x") && props.ContainsKey(key + "_y") && props.ContainsKey(key + "_z"))
            {
                return new Vector3(
                    Convert.ToSingle(props[key + "_x"]),
                    Convert.ToSingle(props[key + "_y"]),
                    Convert.ToSingle(props[key + "_z"]));
            }
        }
        catch (Exception)
        {
        }

        return null;
    }

    private float? GetFloatFromProps(Godot.Collections.Dictionary props, string key)
    {
        if (props == null)
            return null;

        try
        {
            if (props.ContainsKey(key))
            {
                var val = props[key];
                try
                {
                    return (float)val;
                }
                catch { }

                try
                {
                    return Convert.ToSingle(val);
                }
                catch { }
            }
        }
        catch (Exception)
        {
        }

        return null;
    }

    public int[] GetEntityIds()
    {
        var ids = new int[_entityById.Count];
        var i = 0;
        foreach (var kv in _entityById)
            ids[i++] = kv.Key;
        return ids;
    }

    public void ClearSpawnedEntities(bool freeNodes = true)
    {
        if (freeNodes)
        {
            foreach (var kv in _entityById)
            {
                if (IsInstanceValid(kv.Value))
                    kv.Value.QueueFree();
            }
        }

        _entityById.Clear();
        _idByEntity.Clear();
        _lastSentTransform.Clear();
        _nextEntityId = 1;
    }

    public override void _Ready()
    {
        try
        {
            _syncRoot = SyncRootPath != null && !SyncRootPath.IsEmpty
                ? GetNode(SyncRootPath)
                : this;
        }
        catch
        {
            _syncRoot = this;
            GD.PushWarning("SceneSync: SyncRootPath not found; defaulting to self.");
        }

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        ReindexPathSyncedNodes();
    }

    public void ReindexPathSyncedNodes()
    {
        _pathEntityByRelPath.Clear();
        _lastSentPathTransform.Clear();

        if (string.IsNullOrWhiteSpace(PathSyncGroup))
            return;

        foreach (var node in GetTree().GetNodesInGroup(PathSyncGroup))
        {
            if (node is not Node3D n3d)
                continue;

            if (!_syncRoot.IsAncestorOf(n3d))
                continue;

            var rel = _syncRoot.GetPathTo(n3d).ToString();
            _pathEntityByRelPath[rel] = n3d;
            _lastSentPathTransform[rel] = n3d.GlobalTransform;
        }
    }

    private void OnPeerConnected(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;

        // Refresh in case the server added path-synced nodes since boot.
        ReindexPathSyncedNodes();

        // Snapshot: tell the joining client about all existing entities.
        foreach (var kv in _entityById)
        {
            var id = kv.Key;
            var node = kv.Value;
            if (!IsInstanceValid(node))
                continue;

            // Players are updated via NetRoot position RPCs (client-authoritative). Avoid double-sync.
            if (node is PlayerAvatar)
                continue;

            var scenePath = node.SceneFilePath;
            var parentPath = _syncRoot.GetPathTo(node.GetParent());
            RpcId(peerId, nameof(ClientSpawnEntity), id, scenePath, parentPath.ToString(), node.GlobalTransform);
        }

        // Snapshot: transforms for pre-existing nodes synced by relative path.
        foreach (var kv in _pathEntityByRelPath)
        {
            var relPath = kv.Key;
            var node = kv.Value;
            if (!IsInstanceValid(node))
                continue;

            RpcId(peerId, nameof(ClientPathTransformSnapshot), relPath, node.GlobalTransform);
        }
    }

    private void OnPeerDisconnected(long peerId)
    {
        // No per-peer cleanup needed for scene entities in this minimal version.
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Multiplayer.IsServer())
            return;

        if (TransformSendHz <= 0)
            return;

        _sendAccumulator += delta;
        var interval = 1.0 / TransformSendHz;
        if (_sendAccumulator < interval)
            return;
        _sendAccumulator = 0;

        foreach (var kv in _entityById)
        {
            var id = kv.Key;
            var node = kv.Value;
            if (!IsInstanceValid(node))
                continue;

            var xform = node.GlobalTransform;
            if (_lastSentTransform.TryGetValue(id, out var last) && last == xform)
                continue;

            _lastSentTransform[id] = xform;
            Rpc(nameof(ClientTransformDelta), id, xform);
        }

        foreach (var kv in _pathEntityByRelPath)
        {
            var relPath = kv.Key;
            var node = kv.Value;
            if (!IsInstanceValid(node))
                continue;

            var xform = node.GlobalTransform;
            if (_lastSentPathTransform.TryGetValue(relPath, out var last) && last == xform)
                continue;

            _lastSentPathTransform[relPath] = xform;
            Rpc(nameof(ClientPathTransformDelta), relPath, xform);
        }
    }

    /// <summary>
    /// Server-only: spawns a PackedScene at the sync root and replicates it.
    /// </summary>
    public int ServerSpawn(string packedScenePath, Transform3D transform, Node? parent = null)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerSpawn is server-only");

        var scene = GD.Load<PackedScene>(packedScenePath);
        if (scene == null)
            throw new InvalidOperationException($"Failed to load PackedScene: {packedScenePath}");

        var node = scene.Instantiate<Node3D>();
        var actualParent = parent ?? _syncRoot;
        actualParent.AddChild(node);
        node.GlobalTransform = transform;

        var id = _nextEntityId++;
        RegisterEntity(id, node);

        // Broadcast to clients (reliable by default).
        var parentPath = _syncRoot.GetPathTo(actualParent);
        Rpc(nameof(ClientSpawnEntity), id, packedScenePath, parentPath.ToString(), transform);

        return id;
    }

    /// <summary>
    /// Server-only: spawn a built-in Node3D (limited whitelist) and replicate it to clients.
    /// Props is a Godot.Collections.Dictionary with simple properties (e.g. for CSGBox3D: { "size": Vector3 }).
    /// </summary>
    public int ServerSpawnBuiltin(string nodeType, Godot.Collections.Dictionary props, Transform3D transform, Node? parent = null)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerSpawnBuiltin is server-only");

        var node = CreateBuiltinNode(nodeType);
        if (node == null)
            throw new InvalidOperationException($"Unsupported builtin node type: {nodeType}");

        ApplyPropsToNode(node, props);

        var actualParent = parent ?? _syncRoot;
        actualParent.AddChild(node);
        node.GlobalTransform = transform;

        var id = _nextEntityId++;
        RegisterEntity(id, node);

        var parentPath = _syncRoot.GetPathTo(actualParent);
        Rpc(nameof(ClientSpawnBuiltin), id, nodeType, props, parentPath.ToString(), transform);

        return id;
    }

    public void ServerDespawn(int entityId)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerDespawn is server-only");

        if (_entityById.TryGetValue(entityId, out var node) && IsInstanceValid(node))
            node.QueueFree();

        UnregisterEntity(entityId);
        Rpc(nameof(ClientDespawnEntity), entityId);
    }

    private void RegisterEntity(int entityId, Node3D node)
    {
        _entityById[entityId] = node;
        _idByEntity[node] = entityId;
        _lastSentTransform[entityId] = node.GlobalTransform;

        // Helpful for debugging in the remote scene tree.
        node.SetMeta("net_entity_id", entityId);
    }

    private Node3D? CreateBuiltinNode(string nodeType)
    {
        // Provide safe, engine-level builtins via MeshInstance3D when possible.
        return nodeType switch
        {
            "Box" => CreateBoxInstance(),
            "Sphere" => CreateSphereInstance(),
            _ => null,
        };
    }

    private MeshInstance3D CreateBoxInstance()
    {
        var mesh = new BoxMesh();
        var inst = new MeshInstance3D();
        inst.Mesh = mesh;
        return inst;
    }

    private MeshInstance3D CreateSphereInstance()
    {
        var mesh = new SphereMesh();
        var inst = new MeshInstance3D();
        inst.Mesh = mesh;
        return inst;
    }

    private void ApplyPropsToNode(Node3D node, Godot.Collections.Dictionary props)
    {
        if (props == null)
            return;

        try
        {
            if (node is MeshInstance3D mi && mi.Mesh is BoxMesh bmesh)
            {
                var v = GetVector3FromProps(props, "size");
                if (v != null)
                    bmesh.Size = v.Value;
            }

            if (node is MeshInstance3D mi2 && mi2.Mesh is SphereMesh smesh)
            {
                var r = GetFloatFromProps(props, "radius");
                if (r.HasValue)
                    smesh.Radius = r.Value;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"ApplyPropsToNode error: {e.Message}");
        }
    }

    private void UnregisterEntity(int entityId)
    {
        if (_entityById.TryGetValue(entityId, out var node))
            _idByEntity.Remove(node);

        _entityById.Remove(entityId);
        _lastSentTransform.Remove(entityId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientSpawnEntity(int entityId, string packedScenePath, string parentPath, Transform3D transform)
    {
        if (_entityById.ContainsKey(entityId))
            return;

        var scene = GD.Load<PackedScene>(packedScenePath);
        if (scene == null)
        {
            GD.PrintErr($"ClientSpawnEntity failed to load: {packedScenePath}");
            return;
        }

        var node = scene.Instantiate<Node3D>();

        Node parent = _syncRoot;
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var maybe = _syncRoot.GetNodeOrNull<Node>(parentPath);
            if (maybe != null)
                parent = maybe;
        }

        parent.AddChild(node);
        node.GlobalTransform = transform;
        RegisterEntity(entityId, node);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientSpawnBuiltin(int entityId, string nodeType, Godot.Collections.Dictionary props, string parentPath, Transform3D transform)
    {
        if (_entityById.ContainsKey(entityId))
            return;

        var node = CreateBuiltinNode(nodeType);
        if (node == null)
        {
            GD.PrintErr($"ClientSpawnBuiltin: unsupported node type: {nodeType}");
            return;
        }

        ApplyPropsToNode(node, props);

        Node parent = _syncRoot;
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            var maybe = _syncRoot.GetNodeOrNull<Node>(parentPath);
            if (maybe != null)
                parent = maybe;
        }

        parent.AddChild(node);
        node.GlobalTransform = transform;
        RegisterEntity(entityId, node);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientDespawnEntity(int entityId)
    {
        if (_entityById.TryGetValue(entityId, out var node) && IsInstanceValid(node))
            node.QueueFree();

        UnregisterEntity(entityId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ClientTransformDelta(int entityId, Transform3D transform)
    {
        if (!_entityById.TryGetValue(entityId, out var node) || !IsInstanceValid(node))
            return;

        // Don't apply deltas to local player's entity, as it's client-authoritative.
        if (node is PlayerAvatar avatar && avatar.IsLocalPlayer)
            return;

        // Don't apply deltas to local VR player's rig, as it's client-authoritative.
        if (node is XrPlayerRig rig && rig.IsLocalPlayer)
            return;

        node.GlobalTransform = transform;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientPathTransformSnapshot(string relPath, Transform3D transform)
    {
        var node = _syncRoot.GetNodeOrNull<Node3D>(relPath);
        if (node == null)
        {
            GD.PrintErr($"SceneSync: snapshot missing node at '{relPath}' under sync root.");
            return;
        }

        node.GlobalTransform = transform;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ClientPathTransformDelta(string relPath, Transform3D transform)
    {
        var node = _syncRoot.GetNodeOrNull<Node3D>(relPath);
        if (node == null)
            return;

        node.GlobalTransform = transform;
    }
}
