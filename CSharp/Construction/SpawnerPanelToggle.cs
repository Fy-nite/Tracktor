#nullable enable
using Godot;
using System;
using Trakction.Interaction;
using Trakction.PhysicalUI;


namespace Trakction.CSharp.Construction;

public partial class SpawnerPanelToggle : Node
{
    [Export]
    public PackedScene? PanelScene { get; set; }
    
    [Export]
    public NodePath ParentPath { get; set; } = new NodePath();

    // Prefer using an InputMap action name (recommended). If that action doesn't exist
    // the script will fall back to the key below.
    [Export]
    public string ToggleAction { get; set; } = "spawn_panel";

    [Export]
    public Key ToggleKey { get; set; } = Key.E;

    // Optional camera wiring: if you set CameraPath to a Camera3D, the spawned panel
    // (if Node3D) will be placed in front of the camera.
    [Export]
    public NodePath CameraPath { get; set; } = new NodePath();

    [Export]
    public float Distance { get; set; } = 2.0f;

    [Export]
    public Vector3 PanelOffset { get; set; } = Vector3.Zero;

    [Export]
    public bool FaceCamera { get; set; } = true;

    // When true, the panel is instantiated once and toggled visible/invisible instead of queued/freeing each time.
    [Export]
    public bool ReuseInstance { get; set; } = true;

    private Node? _parentNode;
    private Node? _panelInstance;
    private Camera3D? _camera;
    private bool _prevToggleKeyDown;
    private bool _prevMouseLeftDown;
    private PhysicalUI.PhysicalScrollBox3D? _scrollBox;

    public override void _Ready()
    {
        // Resolve parent; prefer explicit path, otherwise use current scene root
        // if (ParentPath.ToString() != "")
        //     _parentNode = GetNodeOrNull(ParentPath);

        if (_parentNode == null)
            _parentNode = GetTree().CurrentScene;

        if (_parentNode == null)
            GD.PrintErr("SpawnerPanelToggle: Could not resolve a parent node to instance the panel into.");

        // Try to resolve camera if a path was provided
        // if (CameraPath.ToString() != "")
        // {
        //     var node = GetNodeOrNull(CameraPath);
        //     if (node is Camera3D cam)
        //         _camera = cam;
        //     else if (node is Node3D nd)
        //         _camera = nd as Camera3D; // will be null if not a camera
        // }

        // If no camera via path, try to locate a Camera3D in the current scene tree
        if (_camera == null)
        {
            var root = GetTree().CurrentScene;
            if (root != null)
                _camera = FindCameraInTree(root);
        }

        // Initialize previous key state
        _prevToggleKeyDown = false;
        _prevMouseLeftDown = false;

        // If reuse is enabled and PanelScene is assigned, pre-instantiate the panel so it's ready
        if (ReuseInstance && PanelScene != null)
        {
            // Instantiate but keep it hidden until toggled on
            if (_panelInstance == null || !IsInstanceValid(_panelInstance))
            {
                _panelInstance = PanelScene.Instantiate();
                _parentNode?.AddChild(_panelInstance);
                SetNodeVisibility(_panelInstance, false);
                // Try to find the scroll box for desktop interaction
                _scrollBox = _panelInstance.GetNodeOrNull<PhysicalUI.PhysicalScrollBox3D>("ScrollPanel");
            }
        }
    }

    public override void _Process(double delta)
    {
        // If an InputMap action exists -> use it. Otherwise fall back to a key press.
        if (!string.IsNullOrEmpty(ToggleAction) && InputMap.HasAction(ToggleAction))
        {
            if (Input.IsActionJustPressed(ToggleAction))
                TogglePanel();
        }
        else
        {
            // Edge detector using IsKeyPressed (safer across API surfaces)
            var down = Input.IsKeyPressed(ToggleKey);
            if (down && !_prevToggleKeyDown)
                TogglePanel();
            _prevToggleKeyDown = down;
        }

        // Handle desktop interaction if panel is visible
        if (_panelInstance != null && IsInstanceValid(_panelInstance) && GetNodeVisibility(_panelInstance))
        {
            HandleDesktopInteraction();
        }
    }

    private void HandleDesktopInteraction()
    {
        if (_camera == null)
            return;

        var viewport = GetViewport();
        if (viewport == null)
            return;

        // Handle mouse click for button presses (edge detector)
        var down = Input.IsMouseButtonPressed(MouseButton.Left);
        var justPressed = down && !_prevMouseLeftDown;
        _prevMouseLeftDown = down;

        if (justPressed)
        {
            var mousePos = viewport.GetMousePosition();
            var rayOrigin = _camera.ProjectRayOrigin(mousePos);
            var rayDirection = _camera.ProjectRayNormal(mousePos);

            var spaceState = _camera.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDirection * 100.0f); // Ray length 100 units
            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                if (result["collider"].Obj is not Node hitNode)
                    return;

                var interactable = FindInteractable(hitNode);
                if (interactable == null)
                    return;

                var hitPos = result.TryGetValue("position", out var p) ? (Vector3)p : Vector3.Zero;
                var hitNormal = result.TryGetValue("normal", out var n) ? (Vector3)n : Vector3.Up;

                var ctx = new InteractionContext(
                    Interactor: _camera,
                    HitNode: hitNode,
                    HitPosition: hitPos,
                    HitNormal: hitNormal,
                    SourceEvent: null
                );

                if (interactable.CanInteract(in ctx))
                    interactable.Interact(in ctx);
            }
        }
    }

    private static IInteractable? FindInteractable(Node start)
    {
        Node? n = start;
        while (n != null)
        {
            if (n is IInteractable i)
                return i;
            n = n.GetParent();
        }
        return null;
    }

    private void TogglePanel()
    {
        if (_panelInstance == null || !IsInstanceValid(_panelInstance))
            ShowPanel();
        else
        {
            // If reuse is enabled, toggle visibility; otherwise hide (free) or show
            if (ReuseInstance)
            {
                // Determine visibility and toggle
                var isVisible = GetNodeVisibility(_panelInstance);
                SetNodeVisibility(_panelInstance, !isVisible);

                // If we're showing it, reposition to be in front of the camera
                if (!isVisible)
                    PositionPanelIfNeeded(_panelInstance);
            }
            else
            {
                // Not reusing: if instance exists and is visible, hide it, else create one
                // We'll use HidePanel to free it
                HidePanel();
            }
        }
    }

    private void ShowPanel()
    {
        if (PanelScene == null)
        {
            GD.PrintErr("SpawnerPanelToggle: PanelScene is not assigned.");
            return;
        }

        // If reuse is enabled, instantiate only once and just show
        if (ReuseInstance)
        {
            if (_panelInstance == null || !IsInstanceValid(_panelInstance))
            {
                _panelInstance = PanelScene.Instantiate();
                _parentNode?.AddChild(_panelInstance);
                // Find scroll box
                _scrollBox = _panelInstance.GetNodeOrNull<PhysicalUI.PhysicalScrollBox3D>("ScrollPanel");
            }

            SetNodeVisibility(_panelInstance, true);
            PositionPanelIfNeeded(_panelInstance);
            return;
        }

        // Default: create new instance and add
        var instance = PanelScene.Instantiate();
        _panelInstance = instance;
        _parentNode?.AddChild(_panelInstance);
        // Find scroll box
        _scrollBox = _panelInstance.GetNodeOrNull<PhysicalUI.PhysicalScrollBox3D>("ScrollPanel");
        PositionPanelIfNeeded(_panelInstance);
    }

    private void HidePanel()
    {
        if (_panelInstance == null || !IsInstanceValid(_panelInstance))
            return;

        if (ReuseInstance)
        {
            SetNodeVisibility(_panelInstance, false);
        }
        else
        {
            _panelInstance.QueueFree();
            _panelInstance = null;
            _scrollBox = null;
        }
    }

    private void PositionPanelIfNeeded(Node instance)
    {
        if (_camera == null)
            return;

        if (instance is Node3D panel3d)
        {
            var camTransform = _camera.GlobalTransform;
            var forward = -camTransform.Basis.Z;
            var targetPos = camTransform.Origin + forward * Distance + PanelOffset;

            var newTransform = panel3d.GlobalTransform;
            newTransform.Origin = targetPos;
            panel3d.GlobalTransform = newTransform;

            if (FaceCamera)
                panel3d.LookAt(_camera.GlobalTransform.Origin, Vector3.Up);
        }
        else if (instance is CanvasItem)
        {
            // For CanvasItem-based panels, we could optionally attempt to position
            // them in front of camera by converting world->screen coordinates. Leave
            // positioning to the scene author for CanvasItem panels.
        }
    }

    private void SetNodeVisibility(Node node, bool visible)
    {
        if (node is CanvasItem ci)
            ci.Visible = visible;
        else if (node is Node3D n3)
            n3.Visible = visible;
        else
        {
            // For unknown node types, enable/disable processing instead
            node.SetProcess(visible);
            node.SetPhysicsProcess(visible);
            foreach (var childObj in node.GetChildren())
                if (childObj is Node child)
                    SetNodeVisibility(child, visible);
        }
    }

    private bool GetNodeVisibility(Node node)
    {
        if (node is CanvasItem ci)
            return ci.Visible;
        if (node is Node3D n3)
            return n3.Visible;
        return node.IsProcessing();
    }

    // Recursive search for a Camera3D in the scene tree. Stops at first match.
    private Camera3D? FindCameraInTree(Node? root)
    {
        if (root == null)
            return null;

        if (root is Camera3D cam)
            return cam;

        foreach (var childObj in root.GetChildren())
        {
            if (childObj is Node child)
            {
                var found = FindCameraInTree(child);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}
