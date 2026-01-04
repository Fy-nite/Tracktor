# Tracktor

Tracktor is a Godot + C# project (workspace "V12") that provides a modular scene/component system and tools
for building interactive 3D experiences. The codebase mixes engine initialization, world management, and a
collection of reusable components (meshes, lights, sky/environment helpers) under the `V12` project.

Quick overview
- Projects: `Tracktor` (root) and `V12` (components/frameworks) — both target .NET 10 in this workspace.
- Engine integration: C# bootstrap files under `CSharp/` (e.g. `EngineInit.cs`, `WorldInit.cs`, `VrPlayer.cs`).
- Scenes: `Scenes/Userspace.cs` and other scene glue code live alongside runtime logic.

V12 highlights
- `V12/Components/` — Component-based building blocks for scene content:
  - `NodeComponent`, `Component` base classes that expose runtime and editor UI integration.
  - Mesh components: `BoxMesh`, etc.
  - Lighting components: `Spotlight`, `DirectionalLight`, `OmniLight`.
  - Environment helpers: `WorldSkybox`, `PanoramaSky` (panorama texture support via `TexturePath`).
- `V12/World/` — World and element management (e.g. `WorldManager`, `WorldElementSceneSyncer`, `ErrorSpace`).
- `V12/Types/` and `V12/Interfaces/` — shared types and interface contracts used across the system.

Addons
- The repository contains several Godot addons under `addons/` used for tooling, XR, and debugging.

Build & run
- Requirements: Godot (with C# support configured for the workspace), .NET SDK 10 installed.
- To build from the command line: `dotnet build Tracktor.csproj` (or open the project in your IDE).
- To run and test scenes, open the project folder in the Godot editor and run the main scene.

Extending the project
- New components follow the `NodeComponent` pattern: expose properties with `InspectorExpose`, defer
  node creation to `RunComponentLogic`, and return the created `Node3D` so it can be parented into the
  world by `NodeComponent.OnAttach()`.

Contact / notes
- This README is a brief summary; see inline source files and comments for implementation details.

