#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

// dopn't import thism it doesn't exist.
// and we already have a different scene for XR players.
// using Traktion.XR;



/*
 *
 * // Spawn a PackedScene
int id = sceneSync.ServerSpawn("res://scenes/MyObject.tscn", transform);

// Spawn a built-in box with custom size
var props = new Godot.Collections.Dictionary { { "size", new Vector3(2, 1, 1) } };
int id = sceneSync.ServerSpawn("Box", transform, parent, props);

// Register and spawn a custom node
SceneSync.RegisterNodeFactory("MyCustomNode", () => new MyCustomNode3D());
int id = sceneSync.ServerSpawn("MyCustomNode", transform);
 */

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

    private static readonly System.Collections.Generic.Dictionary<string, Func<Node3D>> _nodeFactories = new()
    {
        ["Box"] = () =>
        {
            var mesh = new BoxMesh();
            var inst = new MeshInstance3D();
            inst.Mesh = mesh;
            return inst;
        },
        ["Sphere"] = () =>
        {
            var mesh = new SphereMesh();
            var inst = new MeshInstance3D();
            inst.Mesh = mesh;
            return inst;
        },
        ["CsgBox3D"] = () => new CsgBox3D(),
    };

    private Node _syncRoot = null!;

    private int _nextEntityId = 1;
    private readonly System.Collections.Generic.Dictionary<int, Node3D> _entityById = new();
    private readonly System.Collections.Generic.Dictionary<Node3D, int> _idByEntity = new();
    private readonly System.Collections.Generic.Dictionary<int, Transform3D> _lastSentTransform = new();

    private readonly System.Collections.Generic.Dictionary<string, Node3D> _pathEntityByRelPath = new();
    private readonly System.Collections.Generic.Dictionary<string, Transform3D> _lastSentPathTransform = new();

    private readonly System.Collections.Generic.Dictionary<int, string> _spawnKeyById = new();

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

    private static string VariantToString(Variant v)
    {
        try
        {
            return v.AsString();
        }
        catch
        {
            return v.ToString();
        }
    }

    private static bool TryGetVector3(Godot.Collections.Dictionary dict, string key, out Vector3 value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsVector3();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetArray(Godot.Collections.Dictionary dict, string key, out Godot.Collections.Array value)
    {
        value = default!;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsGodotArray();
            return value != null;
        }
        catch
        {
            return false;
        }
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
        _spawnKeyById.Clear();
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

        // One-shot mirror of static hierarchy for late join.
        if (MirrorHierarchyOnJoin)
        {
            try
            {
                // Mirror only mirrorable children under _syncRoot.
                var container = new Node3D { Name = "MirroredRoot" };
                foreach (var childObj in _syncRoot.GetChildren())
                {
                    if (childObj is not Node child)
                        continue;
                    if (!IsMirrorable(child))
                        continue;

                    // Duplicate so we don't mutate the live scene while serializing.
                    var dup = child.Duplicate(flags: (int)Node.DuplicateFlags.UseInstantiation) as Node;
                    if (dup == null)
                        continue;
                    container.AddChild(dup);
                }

                var snapshot = SerializeMirroredSubtree(container);
                container.QueueFree();

                RpcId(peerId, nameof(ClientApplyMirroredHierarchy), snapshot);
            }
            catch (Exception e)
            {
                GD.PrintErr($"MirrorHierarchyOnJoin failed: {e.Message}");
            }
        }

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

            var parentPath = _syncRoot.GetPathTo(node.GetParent());
            RpcId(peerId, nameof(ClientSpawnEntity), id, _spawnKeyById[id], parentPath.ToString(), node.GlobalTransform,
                new Godot.Collections.Dictionary());
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
    /// Server-only: spawns a node using a spawn key (PackedScene path or factory key) and replicates it.
    /// For PackedScenes, use "res://path/to/scene.tscn". For built-ins or custom, use factory key.
    /// Props is a Godot.Collections.Dictionary with simple properties.
    /// </summary>
    public int ServerSpawn(string spawnKey, Transform3D transform, Node? parent = null,
        Godot.Collections.Dictionary? props = null)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerSpawn is server-only");

        Node3D node;
        if (spawnKey.StartsWith("res://"))
        {
            var scene = GD.Load<PackedScene>(spawnKey);
            if (scene == null)
                throw new InvalidOperationException($"Failed to load PackedScene: {spawnKey}");

            node = scene.Instantiate<Node3D>();
        }
        else
        {
            if (!_nodeFactories.TryGetValue(spawnKey, out var factory))
                throw new InvalidOperationException($"Unknown spawn key: {spawnKey}");

            node = factory();
        }

        if (props == null)
            props = new Godot.Collections.Dictionary();

        var actualParent = parent ?? _syncRoot;
        actualParent.AddChild(node);
        node.GlobalTransform = transform;

        var id = _nextEntityId++;
        _spawnKeyById[id] = spawnKey;
        RegisterEntity(id, node);

        // Broadcast to clients (reliable by default).
        var parentPath = _syncRoot.GetPathTo(actualParent);
        Rpc(nameof(ClientSpawnEntity), id, spawnKey, parentPath.ToString(), transform, props);

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

    private Vector3? GetVector3FromProps(Dictionary props, string key)
    {
        if (props.ContainsKey(key))
        {
            try
            {
                return ((Variant)props[key]).AsVector3();
            }
            catch
            {
                GD.PrintErr($"GetVector3FromProps: invalid Vector3 for key '{key}'");
            }
        }

        return null;
    }

    private float? GetFloatFromProps(Dictionary props, string key)
    {
        if (props.ContainsKey(key))
        {
            try
            {
                return ((Variant)props[key]).AsSingle();
            }
            catch
            {
                GD.PrintErr($"GetFloatFromProps: invalid float for key '{key}'");
            }
        }

        return null;
    }

    private void UnregisterEntity(int entityId)
    {
        if (_entityById.TryGetValue(entityId, out var node))
            _idByEntity.Remove(node);

        _entityById.Remove(entityId);
        _lastSentTransform.Remove(entityId);
        _spawnKeyById.Remove(entityId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientSpawnEntity(int entityId, string spawnKey, string parentPath, Transform3D transform,
        Godot.Collections.Dictionary props)
    {
        if (_entityById.ContainsKey(entityId))
            return;

        Node3D node;
        if (spawnKey.StartsWith("res://"))
        {
            var scene = GD.Load<PackedScene>(spawnKey);
            if (scene == null)
            {
                GD.PrintErr($"ClientSpawnEntity failed to load: {spawnKey}");
                return;
            }

            node = scene.Instantiate<Node3D>();
        }
        else
        {
            if (!_nodeFactories.TryGetValue(spawnKey, out var factory))
            {
                GD.PrintErr($"ClientSpawnEntity unknown spawn key: {spawnKey}");
                return;
            }

            node = factory();
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
        if (node is Trakction.XR.XrPlayerRig rig && rig.IsLocalPlayer)
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

    // --- Static hierarchy mirroring (one-shot snapshot on join) ---
    [Export] public bool MirrorHierarchyOnJoin = true;

    [Export] public string MirrorFactoryMetaKey = "net_factory";

    // Stable IDs for mirrored nodes (separate from _entityById which is for spawned entities).
    private long _nextMirroredId = 1;
    private readonly System.Collections.Generic.Dictionary<Node3D, long> _mirroredIdByNode = new();
    private readonly System.Collections.Generic.Dictionary<long, Node3D> _mirroredNodeById = new();

    private long GetOrAssignMirroredId(Node3D node)
    {
        if (_mirroredIdByNode.TryGetValue(node, out var id) && IsInstanceValid(node))
            return id;

        id = _nextMirroredId++;
        _mirroredIdByNode[node] = id;
        return id;
    }

    private void ClearClientMirroredIndex()
    {
        _mirroredNodeById.Clear();
    }

    private void IndexClientMirroredTree(Node root)
    {
        // Walk and index any nodes with meta 'net_mirror_id' (we set it during instantiation)
        if (root is Node3D n3d && root.HasMeta("net_mirror_id"))
        {
            try
            {
                var id = (long)root.GetMeta("net_mirror_id");
                _mirroredNodeById[id] = n3d;
            }
            catch { }
        }

        foreach (var childObj in root.GetChildren())
        {
            if (childObj is Node child)
                IndexClientMirroredTree(child);
        }
    }

    private static bool HasScenePath(Node node)
        => node is Node3D && !string.IsNullOrWhiteSpace(node.SceneFilePath) &&
           node.SceneFilePath.StartsWith("res://", StringComparison.Ordinal);

    private bool IsMirrorable(Node node)
        => node is Node3D && (HasScenePath(node) || TryGetFactoryKey(node, out _));

    private Godot.Collections.Dictionary SerializeMirroredSubtree(Node node)
    {
        // Dictionary layout:
        // {
        //   "id": int64,
        //   "name": "NodeName",
        //   "scene": "res://path/to/thing.tscn" | "",
        //   "factory": "Box" | ""  (requires meta net_factory)
        //   "p": Vector3 (position), "r": Vector3 (rotation), "s": Vector3 (scale), "v": bool
        //   "props": Dictionary (optional)
        //   "c": Array<Dictionary> children
        // }

        long id = 0;
        if (node is Node3D n3dId)
            id = GetOrAssignMirroredId(n3dId);

        var dict = new Godot.Collections.Dictionary
        {
            ["id"] = id,
            ["name"] = node.Name,
        };

        if (HasScenePath(node))
        {
            dict["scene"] = node.SceneFilePath;
            dict["factory"] = "";
        }
        else if (TryGetFactoryKey(node, out var factoryKey))
        {
            dict["scene"] = "";
            dict["factory"] = factoryKey;
        }
        else
        {
            dict["scene"] = "";
            dict["factory"] = "";
        }

        if (node is Node3D n3d)
        {
            dict["p"] = n3d.Position;
            dict["r"] = n3d.Rotation;
            dict["s"] = n3d.Scale;
            dict["v"] = n3d.Visible;
        }

        dict["props"] = new Godot.Collections.Dictionary();

        var children = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var childObj in node.GetChildren())
        {
            if (childObj is not Node child)
                continue;

            if (!IsMirrorable(child))
                continue;

            children.Add(SerializeMirroredSubtree(child));
        }

        dict["c"] = children;
        return dict;
    }

    private static bool TryGetInt64(Godot.Collections.Dictionary dict, string key, out long value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsInt64();
            return true;
        }
        catch
        {
            try
            {
                value = Convert.ToInt64(dict[key]);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private Node3D? InstantiateMirroredNode(Godot.Collections.Dictionary snapshot)
    {
        long id = 0;
        TryGetInt64(snapshot, "id", out id);

        string name = snapshot.ContainsKey("name") ? VariantToString((Variant)snapshot["name"]) : "";
        string scenePath = snapshot.ContainsKey("scene") ? VariantToString((Variant)snapshot["scene"]) : "";
        string factoryKey = snapshot.ContainsKey("factory") ? VariantToString((Variant)snapshot["factory"]) : "";

        Node3D node;
        if (!string.IsNullOrWhiteSpace(scenePath))
        {
            if (!scenePath.StartsWith("res://", StringComparison.Ordinal))
            {
                GD.PrintErr($"MirrorHierarchy: invalid scene path '{scenePath}'");
                return null;
            }

            var scene = GD.Load<PackedScene>(scenePath);
            if (scene == null)
            {
                GD.PrintErr($"MirrorHierarchy: failed to load scene '{scenePath}'");
                return null;
            }

            node = scene.Instantiate<Node3D>();
        }
        else if (!string.IsNullOrWhiteSpace(factoryKey))
        {
            if (!_nodeFactories.TryGetValue(factoryKey, out var factory))
            {
                GD.PrintErr($"MirrorHierarchy: unknown factory key '{factoryKey}' (did you RegisterNodeFactory?)");
                return null;
            }

            node = factory();

            if (!string.IsNullOrWhiteSpace(MirrorFactoryMetaKey))
                node.SetMeta(MirrorFactoryMetaKey, factoryKey);
        }
        else
        {
            GD.PrintErr("MirrorHierarchy: snapshot missing both 'scene' and 'factory'");
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name))
            node.Name = name;

        if (TryGetVector3(snapshot, "p", out var p))
            node.Position = p;
        if (TryGetVector3(snapshot, "r", out var r))
            node.Rotation = r;
        if (TryGetVector3(snapshot, "s", out var s))
            node.Scale = s;
        // if (TryGetBool(snapshot, "v", out var vis))
        //     node.Visible = vis;

        // Store ID on the node so we can index later.
        if (id != 0)
        {
            node.SetMeta("net_mirror_id", id);
            _mirroredNodeById[id] = node;
        }

        if (TryGetArray(snapshot, "c", out var arr))
        {
            foreach (var entry in arr)
            {
                Godot.Collections.Dictionary? childSnap = null;
                try { childSnap = entry.AsGodotDictionary(); } catch { }
                if (childSnap == null)
                    continue;

                var child = InstantiateMirroredNode(childSnap);
                if (child == null)
                    continue;

                node.AddChild(child);
            }
        }

        return node;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientApplyMirroredHierarchy(Godot.Collections.Dictionary snapshot)
    {
        if (Multiplayer.IsServer())
            return;

        foreach (var child in _syncRoot.GetChildren())
        {
            if (child is Node n && !(child is SceneSync))
                n.QueueFree();
        }

        ClearClientMirroredIndex();

        try
        {
            var rootNode = InstantiateMirroredNode(snapshot);
            if (rootNode != null)
            {
                _syncRoot.AddChild(rootNode);
                IndexClientMirroredTree(rootNode);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"ClientApplyMirroredHierarchy error: {e.Message}");
        }

        ReindexPathSyncedNodes();
    }

    /// <summary>
    /// Server-only: mirror-add a node and its mirrorable subtree to all clients.
    /// The node must be mirrorable (scene instanced or has net_factory meta).
    /// </summary>
    public void ServerMirrorAdd(Node3D node, Node? parentOverride = null)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerMirrorAdd is server-only");

        if (!IsMirrorable(node))
            throw new InvalidOperationException($"ServerMirrorAdd: node '{node.Name}' is not mirrorable (needs SceneFilePath or meta '{MirrorFactoryMetaKey}')");

        var parent = parentOverride ?? node.GetParent();
        var parentId = 0L;
        if (parent is Node3D p3d)
            parentId = GetOrAssignMirroredId(p3d);

        var snap = SerializeMirroredSubtree(node);
        var id = GetOrAssignMirroredId(node);
        Rpc(nameof(ClientMirrorAdd), id, parentId, snap);
    }

    /// <summary>
    /// Server-only: mirror-remove a previously mirrored node by id.
    /// </summary>
    public void ServerMirrorRemove(long mirrorId)
    {
        if (!Multiplayer.IsServer())
            throw new InvalidOperationException("ServerMirrorRemove is server-only");

        Rpc(nameof(ClientMirrorRemove), mirrorId);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientMirrorAdd(long mirrorId, long parentMirrorId, Godot.Collections.Dictionary snapshot)
    {
        if (Multiplayer.IsServer())
            return;

        // If we already have this node, ignore.
        if (mirrorId != 0 && _mirroredNodeById.ContainsKey(mirrorId))
            return;

        Node parent = _syncRoot;
        if (parentMirrorId != 0 && _mirroredNodeById.TryGetValue(parentMirrorId, out var p))
            parent = p;

        var node = InstantiateMirroredNode(snapshot);
        if (node == null)
            return;

        parent.AddChild(node);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientMirrorRemove(long mirrorId)
    {
        if (Multiplayer.IsServer())
            return;

        if (mirrorId == 0)
            return;

        if (_mirroredNodeById.TryGetValue(mirrorId, out var node) && IsInstanceValid(node))
        {
            node.QueueFree();
        }

        _mirroredNodeById.Remove(mirrorId);
    }

    private bool TryGetFactoryKey(Node node, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(MirrorFactoryMetaKey))
            return false;

        if (!node.HasMeta(MirrorFactoryMetaKey))
            return false;

        try
        {
            var v = node.GetMeta(MirrorFactoryMetaKey);
            if (v.VariantType == Variant.Type.String)
            {
                key = v.AsString();
                return !string.IsNullOrWhiteSpace(key);
            }

            key = v.ToString();
            return !string.IsNullOrWhiteSpace(key);
        }
        catch
        {
            return false;
        }
    }
}
