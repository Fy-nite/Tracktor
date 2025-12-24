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
        ["Box"] = () => { var mesh = new BoxMesh(); var inst = new MeshInstance3D(); inst.Mesh = mesh; return inst; },
        ["Sphere"] = () => { var mesh = new SphereMesh(); var inst = new MeshInstance3D(); inst.Mesh = mesh; return inst; },
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
        try { return v.AsString(); }
        catch { return v.ToString(); }
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
                // Prefer mirroring children under sync root rather than the sync root itself.
                // If you want to mirror a single generated root (like HomeWorld.GenerateWorld()),
                // put it under a dedicated SyncRootPath container.
                var container = new Node3D { Name = "MirroredRoot" };
                foreach (var child in _syncRoot.GetChildren())
                {
                    if (child is not Node ch)
                        continue;
                    if (ch is not Node3D)
                        continue;
                    var typeName = ch.GetType().Name;
                    if (!_mirroredTypeAllowList.Contains(typeName))
                        continue;
                    container.AddChild(ch.Duplicate() as Node);
                }

                // Serialize from the temporary container so we don't leak SceneSync/NetRoot, etc.
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
            RpcId(peerId, nameof(ClientSpawnEntity), id, _spawnKeyById[id], parentPath.ToString(), node.GlobalTransform, new Godot.Collections.Dictionary());
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
    public int ServerSpawn(string spawnKey, Transform3D transform, Node? parent = null, Godot.Collections.Dictionary? props = null)
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
    private void ClientSpawnEntity(int entityId, string spawnKey, string parentPath, Transform3D transform, Godot.Collections.Dictionary props)
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

    // Allow-list of node types we will instantiate when mirroring hierarchy.
    // Keep this tight for safety.
    private static readonly HashSet<string> _mirroredTypeAllowList = new(StringComparer.Ordinal)
    {
        nameof(Node3D),
        nameof(Marker3D),

        // Common gameplay nodes
        nameof(StaticBody3D),
        nameof(RigidBody3D),
        nameof(CharacterBody3D),
        nameof(Area3D),

        // Rendering
        nameof(MeshInstance3D),
        nameof(CpuParticles3D),
        nameof(GpuParticles3D),

        // Lighting
        nameof(OmniLight3D),
        nameof(SpotLight3D),
        nameof(DirectionalLight3D),

        // Collisions
        nameof(CollisionShape3D),
        nameof(CollisionPolygon3D),

        // CSG (common for prototyping/world building)
        nameof(CsgBox3D),
        nameof(CsgSphere3D),
        nameof(CsgCylinder3D),
        nameof(CsgTorus3D),
    };

    private Godot.Collections.Dictionary SerializeMirroredSubtree(Node node)
    {
        // Dictionary layout:
        // {
        //   "t": "Node3D" | "CsgBox3D",
        //   "n": "Name",
        //   "p": Vector3 (position),
        //   "r": Vector3 (rotation),
        //   "s": Vector3 (scale),
        //   "c": Array<Dictionary> children
        // }

        var dict = new Godot.Collections.Dictionary();
        dict["t"] = node.GetType().Name;
        dict["n"] = node.Name;

        if (node is Node3D n3d)
        {
            dict["p"] = n3d.Position;
            dict["r"] = n3d.Rotation;
            dict["s"] = n3d.Scale;
            dict["v"] = n3d.Visible;
        }

        // Minimal per-type properties (safe + common)
        if (node is MeshInstance3D mi)
        {
            // Resource references are safe to send as-is (they'll arrive as Variants).
            dict["mesh"] = mi.Mesh;
            dict["mat0"] = mi.GetSurfaceOverrideMaterial(0);
        }

        if (node is VisualInstance3D vi)
        {
            dict["layers"] = vi.Layers;
        }

        if (node is Light3D light)
        {
            dict["lc"] = light.LightColor;
            dict["le"] = light.LightEnergy;
            dict["sh"] = light.ShadowEnabled;
        }

        if (node is CollisionShape3D cs)
        {
            dict["shape"] = cs.Shape;
            dict["disabled"] = cs.Disabled;
        }

        if (node is CollisionPolygon3D cp)
        {
            dict["depth"] = cp.Depth;
            dict["polygon"] = cp.Polygon;
            dict["disabled"] = cp.Disabled;
        }

        // CSG primitives
        if (node is CsgPrimitive3D csg)
        {
            dict["csg_op"] = (int)csg.Operation;
            dict["csg_snap"] = csg.Snap;
            // dict["csg_smooth"] = csg.SmoothFaces;
            dict["csg_mat"] = csg.MaterialOverride;
        }

        if (node is CsgBox3D csgBox)
            dict["size"] = csgBox.Size;
        if (node is CsgSphere3D csgSphere)
            dict["radius"] = csgSphere.Radius;
        if (node is CsgCylinder3D csgCyl)
        {
            dict["radius"] = csgCyl.Radius;
            dict["height"] = csgCyl.Height;
        }
        // if (node is CsgCapsule3D csgCap)
        // {
        //     dict["radius"] = csgCap.Radius;
        //     dict["height"] = csgCap.Height;
        // }
        if (node is CsgTorus3D csgTor)
        {
            dict["inner_radius"] = csgTor.InnerRadius;
            dict["outer_radius"] = csgTor.OuterRadius;
        }

        var children = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var child in node.GetChildren())
        {
            if (child is not Node ch)
                continue;

            // Only mirror spatial-ish nodes.
            if (ch is not Node3D)
                continue;

            var typeName = ch.GetType().Name;
            if (!_mirroredTypeAllowList.Contains(typeName))
                continue;

            children.Add(SerializeMirroredSubtree(ch));
        }

        dict["c"] = children;
        return dict;
    }

    private static bool TryGetBool(Godot.Collections.Dictionary dict, string key, out bool value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsBool();
            return true;
        }
        catch { return false; }
    }

    private static bool TryGetInt(Godot.Collections.Dictionary dict, string key, out int value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = (int)((Variant)dict[key]).AsInt64();
            return true;
        }
        catch { return false; }
    }

    private static bool TryGetFloat(Godot.Collections.Dictionary dict, string key, out float value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsSingle();
            return true;
        }
        catch { return false; }
    }

    private static bool TryGetColor(Godot.Collections.Dictionary dict, string key, out Color value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsColor();
            return true;
        }
        catch { return false; }
    }

    private static bool TryGetUInt(Godot.Collections.Dictionary dict, string key, out uint value)
    {
        value = default;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = (uint)((Variant)dict[key]).AsUInt64();
            return true;
        }
        catch { return false; }
    }

    private static bool TryGetResource<T>(Godot.Collections.Dictionary dict, string key, out T? value) where T : Resource
    {
        value = null;
        if (!dict.ContainsKey(key))
            return false;

        try
        {
            value = ((Variant)dict[key]).AsGodotObject() as T;
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    private Node InstantiateMirroredNode(Godot.Collections.Dictionary snapshot)
    {
        var typeName = snapshot.ContainsKey("t") ? VariantToString((Variant)snapshot["t"]) : "";
        var name = snapshot.ContainsKey("n") ? VariantToString((Variant)snapshot["n"]) : "";

        if (string.IsNullOrWhiteSpace(typeName) || !_mirroredTypeAllowList.Contains(typeName))
            throw new InvalidOperationException($"MirrorHierarchy: type '{typeName}' not allowed");

        Node node = typeName switch
        {
            nameof(Node3D) => new Node3D(),
            nameof(Marker3D) => new Marker3D(),

            nameof(StaticBody3D) => new StaticBody3D(),
            nameof(RigidBody3D) => new RigidBody3D(),
            nameof(CharacterBody3D) => new CharacterBody3D(),
            nameof(Area3D) => new Area3D(),

            nameof(MeshInstance3D) => new MeshInstance3D(),
            nameof(CpuParticles3D) => new CpuParticles3D(),
            nameof(GpuParticles3D) => new GpuParticles3D(),

            nameof(OmniLight3D) => new OmniLight3D(),
            nameof(SpotLight3D) => new SpotLight3D(),
            nameof(DirectionalLight3D) => new DirectionalLight3D(),

            nameof(CollisionShape3D) => new CollisionShape3D(),
            nameof(CollisionPolygon3D) => new CollisionPolygon3D(),

            nameof(CsgBox3D) => new CsgBox3D(),
            nameof(CsgSphere3D) => new CsgSphere3D(),
            nameof(CsgCylinder3D) => new CsgCylinder3D(),
            // nameof(CsgCapsule3D) => new CsgCapsule3D(),
            nameof(CsgTorus3D) => new CsgTorus3D(),

            _ => throw new InvalidOperationException($"MirrorHierarchy: unsupported type '{typeName}'"),
        };

        if (!string.IsNullOrWhiteSpace(name))
            node.Name = name;

        if (node is Node3D n3d)
        {
            if (TryGetVector3(snapshot, "p", out var p))
                n3d.Position = p;
            if (TryGetVector3(snapshot, "r", out var r))
                n3d.Rotation = r;
            if (TryGetVector3(snapshot, "s", out var s))
                n3d.Scale = s;
            if (TryGetBool(snapshot, "v", out var vis))
                n3d.Visible = vis;
        }

        if (node is VisualInstance3D vi)
        {
            if (TryGetUInt(snapshot, "layers", out var layers))
                vi.Layers = layers;
        }

        if (node is MeshInstance3D mi)
        {
            if (TryGetResource<Mesh>(snapshot, "mesh", out var mesh))
                mi.Mesh = mesh;
            if (TryGetResource<Material>(snapshot, "mat0", out var mat0))
                mi.SetSurfaceOverrideMaterial(0, mat0);
        }

        if (node is Light3D light)
        {
            if (TryGetColor(snapshot, "lc", out var c))
                light.LightColor = c;
            if (TryGetFloat(snapshot, "le", out var e))
                light.LightEnergy = e;
            // if (TryGetFloat(snapshot, "lr", out var range))
                // light.LightRange = range;
            if (TryGetBool(snapshot, "sh", out var sh))
                light.ShadowEnabled = sh;
        }

        if (node is CollisionShape3D cs)
        {
            if (TryGetResource<Shape3D>(snapshot, "shape", out var shape))
                cs.Shape = shape;
            if (TryGetBool(snapshot, "disabled", out var disabled))
                cs.Disabled = disabled;
        }

        if (node is CollisionPolygon3D cp)
        {
            if (TryGetFloat(snapshot, "depth", out var depth))
                cp.Depth = depth;
            if (snapshot.ContainsKey("polygon"))
            {
                try { cp.Polygon = ((Variant)snapshot["polygon"]).AsVector2Array(); }
                catch { }
            }
            if (TryGetBool(snapshot, "disabled", out var disabled))
                cp.Disabled = disabled;
        }

        if (node is CsgPrimitive3D csg)
        {
            if (TryGetInt(snapshot, "csg_op", out var op))
                csg.Operation = (CsgShape3D.OperationEnum)op;
            if (TryGetFloat(snapshot, "csg_snap", out var snap))
                csg.Snap = snap;
            // if (TryGetBool(snapshot, "csg_smooth", out var smooth))
                // csg.SmoothFaces = smooth;
            if (TryGetResource<Material>(snapshot, "csg_mat", out var mat))
                csg.MaterialOverride = mat;
        }

        if (node is CsgBox3D csgBox)
        {
            if (TryGetVector3(snapshot, "size", out var size))
                csgBox.Size = size;
        }
        if (node is CsgSphere3D csgSphere)
        {
            if (TryGetFloat(snapshot, "radius", out var radius))
                csgSphere.Radius = radius;
        }
        if (node is CsgCylinder3D csgCyl)
        {
            if (TryGetFloat(snapshot, "radius", out var radius))
                csgCyl.Radius = radius;
            if (TryGetFloat(snapshot, "height", out var height))
                csgCyl.Height = height;
        }
        // if (node is CsgCapsule3D csgCap)
        // {
        //     if (TryGetFloat(snapshot, "radius", out var radius))
        //         csgCap.Radius = radius;
        //     if (TryGetFloat(snapshot, "height", out var height))
        //         csgCap.Height = height;
        // }
        if (node is CsgTorus3D csgTor)
        {
            if (TryGetFloat(snapshot, "inner_radius", out var inner))
                csgTor.InnerRadius = inner;
            if (TryGetFloat(snapshot, "outer_radius", out var outer))
                csgTor.OuterRadius = outer;
        }

        foreach (var childSnap in snapshot.Values)
        {
            Node child;
            try { child = InstantiateMirroredNode(childSnap.AsGodotDictionary()); }
            catch (Exception e)
            {
                GD.PrintErr($"InstantiateMirroredNode child error: {e.Message}");
                continue;
            }

            node.AddChild(child);
        }

        return node;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void ClientApplyMirroredHierarchy(Godot.Collections.Dictionary snapshot)
    {
        // Client-only: rebuild the mirrored hierarchy under _syncRoot.
        // We only do this once; if you need incremental changes later, add patch messages.
        if (Multiplayer.IsServer())
            return;

        // Clear previous children (but keep SceneSync and any other non-mirrored essentials if SyncRoot is self).
        // If SyncRootPath points to a dedicated container node, this is safe.
        foreach (var child in _syncRoot.GetChildren())
        {
            if (child is Node n)
                n.QueueFree();
        }

        try
        {
            var rootNode = InstantiateMirroredNode(snapshot);
            _syncRoot.AddChild(rootNode);
        }
        catch (Exception e)
        {
            GD.PrintErr($"ClientApplyMirroredHierarchy error: {e.Message}");
        }

        // Reindex any path-synced nodes that now exist.
        ReindexPathSyncedNodes();
    }

    /// <summary>
    /// Registers a custom node factory for spawning.
    /// </summary>
    public static void RegisterNodeFactory(string key, Func<Node3D> factory)
    {
        _nodeFactories[key] = factory;
    }
}
