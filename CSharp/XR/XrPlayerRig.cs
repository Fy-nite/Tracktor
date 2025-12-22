#nullable enable
using System;
using Godot;
using Trakction.Interaction;

namespace Trakction.XR;

/// <summary>
/// Minimal XR player rig: XROrigin3D (current) + XRCamera3D + L/R XRController3D.
/// Attaches an InteractorRay3D to each controller so you can reuse the physical UI interaction system.
/// </summary>
public partial class XrPlayerRig : XROrigin3D
{
    public long PeerId { get; set; }
    public bool IsLocalPlayer { get; set; }

    [Export] public bool EnsureChildren { get; set; } = true;

    [Export] public float RayMaxDistance { get; set; } = 3.0f;

    // OpenXR action map button names (these are the action names exposed by the XR interface).
    // If these are empty, the rays will fall back to InputMap-driven actions.
    [Export] public string GripButtonName { get; set; } = "grip_click";
    [Export] public string TriggerButtonName { get; set; } = "trigger_click";

    // InputMap fallback actions (non-XR or if you prefer mapping in InputMap).
    [Export] public string GrabAction { get; set; } = "grab";

    // The interaction action is still driven by Godot's InputMap.
    // You can map this to a VR controller trigger via the OpenXR action map / input bindings.
    [Export] public string InteractAction { get; set; } = "ui_accept";

    public override void _Ready()
    {
        _net = FindNetRoot(this);
        Current = true;

        if (!EnsureChildren)
            return;

        EnsureRig();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsLocalPlayer)
        {
            if (_camera != null)
                _camera.Current = false;
            return;
        }

        EnsureLocalVrActive();

        // Send position update to server periodically, similar to PlayerAvatar.
        // Use a counter to send at ~12 Hz.
        _sendCounter++;
        if (_sendCounter % 5 != 0)
            return;

        var pos = GlobalPosition;
        if (!IsFinite(pos) || pos.Length() > MaxSendPositionMagnitude)
            return;

        _net?.SendPlayerPosition(pos, GlobalRotation);
    }

    private void EnsureLocalVrActive()
    {
        if (_camera != null)
            _camera.Current = true;
    }

    private NetRoot? _net;
    private XRCamera3D? _camera;
    private int _sendCounter;
    private const float MaxSendPositionMagnitude = 5000.0f;

    private static NetRoot? FindNetRoot(Node? start)
    {
        var node = start;
        while (node != null)
        {
            if (node is NetRoot nr)
                return nr;
            node = node.GetParent();
        }

        // Fallback: locate via group.
        var tree = Godot.Engine.GetMainLoop() as SceneTree;
        if (tree == null)
            return null;

        foreach (var n in tree.GetNodesInGroup("net_root"))
        {
            if (n is NetRoot nr)
                return nr;
        }

        return null;
    }

    private void EnsureRig()
    {
        EnsureCamera();
        EnsureController("LeftController", tracker: "left_hand", pose: "aim");
        EnsureController("RightController", tracker: "right_hand", pose: "aim");
    }

    private void EnsureCamera()
    {
        _camera = GetNodeOrNull<XRCamera3D>("XRCamera3D");
        if (_camera != null)
            return;

        _camera = new XRCamera3D { Name = "XRCamera3D" };
        AddChild(_camera);
    }

    private void EnsureController(string name, string tracker, string pose)
    {
        var controller = GetNodeOrNull<XRController3D>(name);
        if (controller == null)
        {
            controller = new XRController3D { Name = name };
            AddChild(controller);
        }

        // XRController3D inherits tracker/pose from XRNode3D.
        controller.Tracker = new StringName(tracker);
        controller.Pose = new StringName(pose);

        // Attach interactor to the controller so rays follow controller tracking.
        var interactor = controller.GetNodeOrNull<InteractorRay3D>("InteractorRay3D");
        if (interactor == null)
        {
            interactor = new InteractorRay3D { Name = "InteractorRay3D" };
            controller.AddChild(interactor);
        }

        var grabber = controller.GetNodeOrNull<GrabberRay3D>("GrabberRay3D");
        if (grabber == null)
        {
            grabber = new GrabberRay3D { Name = "GrabberRay3D" };
            controller.AddChild(grabber);
        }

        interactor.InteractAction = InteractAction;
        interactor.MaxDistance = RayMaxDistance;

        grabber.GrabAction = GrabAction;
        grabber.MaxDistance = RayMaxDistance;

        WireControllerButtons(controller, interactor, grabber);
    }

    private void WireControllerButtons(XRController3D controller, InteractorRay3D interactor, GrabberRay3D grabber)
    {
        // Avoid duplicate connections when EnsureController runs multiple times.
        const string metaKey = "trakction_xr_button_wired";
        if (controller.HasMeta(metaKey) && controller.GetMeta(metaKey).AsBool())
            return;

        controller.SetMeta(metaKey, true);

        if (!string.IsNullOrWhiteSpace(TriggerButtonName))
        {
            controller.Connect(
                "button_pressed",
                Callable.From((string buttonName) =>
                {
                    if (string.Equals(buttonName, TriggerButtonName, StringComparison.OrdinalIgnoreCase))
                        interactor.TryInteract();
                })
            );
        }

        if (!string.IsNullOrWhiteSpace(GripButtonName))
        {
            controller.Connect(
                "button_pressed",
                Callable.From((string buttonName) =>
                {
                    if (string.Equals(buttonName, GripButtonName, StringComparison.OrdinalIgnoreCase))
                        grabber.TryGrab();
                })
            );

            controller.Connect(
                "button_released",
                Callable.From((string buttonName) =>
                {
                    if (string.Equals(buttonName, GripButtonName, StringComparison.OrdinalIgnoreCase))
                        grabber.Release();
                })
            );
        }
    }

    private static bool IsFinite(Vector3 v)
        => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}
