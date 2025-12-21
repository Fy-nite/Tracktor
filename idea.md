# 3D Collaborative ObjectIR Game Architecture Plan

## Overview

This document outlines the architecture for a collaborative 3D environment using Godot with .NET 8 support, featuring VR and desktop clients, ObjectIR integration, and a headless server. The system supports authoritative server logic, optional client-side execution, and P2P offloading for heavy tasks or media.

---

## 1. Components

### 1.1 Clients (Godot)

* Render world, UI, and panels.
* Deserialize world and ObjectIR modules from server.
* Optional local execution of ObjectIR modules (desynced).
* Send edits via OT/CRDT to server.
* Receive RPC calls from server for authoritative updates.

### 1.2 Headless Server (Godot)

* Full scene tree active, but no rendering or GUI nodes.
* Maintains authoritative state:

  * World objects
  * Panels and UI
  * ObjectIR modules
  * OT/CRDT state for collaborative editing
* Processes edits from clients.
* Sends authoritative updates via RPC and networked messages.
* Optionally communicates with a dedicated ObjectIR runtime for heavy computations.

### 1.3 Optional C++ ObjectIR Runtime

* Dedicated server executable for heavy ObjectIR evaluation.
* Maintains VM state and AST/IR representation.
* Exposes results and VM tree via TCP/gRPC/FlatBuffers.
* Server sends ObjectIR modules for evaluation and receives results.

### 1.4 P2P Components

* WebRTC for media streams (voice/video/screenshare).
* Optional computation offload for distributed ObjectIR graph evaluation.

---

## 2. Data Flow

```
[Clients - Godot]
 ├─ User edits ObjectIR
 └─ Receive world + execution deltas

       │ OT/CRDT + ObjectIR module
       ▼

[Godot Headless Server]
 ├─ World + panels + OT authority
 ├─ Sends ObjectIR module to C++ runtime (optional)
 └─ Receives execution results → broadcasts to clients via RPC/network

       │ TCP / gRPC / FlatBuffers
       ▼

[C++ ObjectIR Runtime Server]
 ├─ VM executes heavy ObjectIR graphs
 ├─ Maintains AST/IR + runtime state
 └─ Streams VM tree / results back

       │ P2P (optional)
       ▼
[Media / heavy computation offload]
```

---

## 3. Scene Sync Strategy

### 3.1 Client Join Sequence

1. Server sends world snapshot:

   * Objects, positions, panels, ownerships, permissions.
2. Client reconstructs world locally.
3. Server sends ObjectIR modules:

   * AST/IR, runtime metadata.
4. Client hooks ObjectIR modules to world objects.
5. Live delta updates begin:

   * Object transforms, ObjectIR edits, panel updates.

### 3.2 Ongoing Synchronization

* Server broadcasts only deltas:

  * Object movements, panel creation/destruction.
  * ObjectIR operations (via OT/CRDT).
  * Ownership and permission changes.
* Clients send only relevant changes back.

### 3.3 Hot-Swap & Recovery

* Hot-swapped modules sent as full state.
* Snapshots used for reconnecting clients or persistence.

---

## 4. Networking Strategy

### 4.1 Godot Multiplayer

* Use Godot's built-in multiplayer API for authoritative state:

  * Reliable channel: ObjectIR edits, panel updates.
  * Unreliable channel: avatar/VR movement, object transforms.
* RPC on clients to push server-authoritative updates.
* Server remains the authority; clients only visualize or optionally simulate.

### 4.2 P2P & Media

* WebRTC for low-latency voice/video/screenshare.
* Optional distributed computation offload for large ObjectIR evaluation.

### 4.3 Optional Dedicated C++ Runtime

* Offload heavy ObjectIR graphs.
* Communicate via TCP/gRPC/FlatBuffers.
* Maintains authoritative VM state.

---

## 5. Headless Server Implementation

* Launch Godot project with `--server` flag:

```csharp
if (OS.GetCmdlineArgs().Contains("--server"))
    StartServer();
else
    StartClient();
```

* Headless server maintains the scene tree, runs scripts, timers, physics.
* No cameras, lights, or rendering nodes.
* Attach a `SceneChangeTracker` script to detect node additions, removals, and transformations.

---

## 6. SceneChangeTracker Example

```csharp
[Remote]
public class SceneChangeTracker : Node
{
    private Dictionary<Node, Transform3D> nodeTransforms = new();

    public override void _Process(double delta)
    {
        foreach (var kvp in new Dictionary<Node, Transform3D>(nodeTransforms))
        {
            Node node = kvp.Key;
            if (!IsInstanceValid(node)) { nodeTransforms.Remove(node); continue; }

            if (node is Node3D n3d && nodeTransforms[node] != n3d.GlobalTransform)
            {
                GD.Print($"Node '{n3d.Name}' moved: {n3d.GlobalTransform}");
                nodeTransforms[node] = n3d.GlobalTransform;
            }
        }
        TrackScene(GetTree().Root);
    }

    private void TrackScene(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (!nodeTransforms.ContainsKey(child))
            {
                nodeTransforms[child] = (child as Node3D)?.GlobalTransform ?? new Transform3D();
                GD.Print($"Node '{child.Name}' added");
            }
            if (child.GetChildCount() > 0) TrackScene(child);
        }
    }
}
```

* Detects node addition, removal, and transform changes.
* Can be extended to track ObjectIR module state changes.

---

## 7. Summary

* Use Godot built-in multiplayer for authoritative world and ObjectIR updates.
* Headless Godot server manages scene tree and networking.
* RPCs on clients enable server-driven updates.
* Optional C++ runtime offloads heavy ObjectIR evaluation, exposing VM state.
* P2P/WebRTC handles media and optional computation offload.
* SceneChangeTracker script monitors scene tree changes for syncing.

This setup ensures deterministic, authoritative state while supporting VR, desktop, and collaborative ObjectIR execution.
