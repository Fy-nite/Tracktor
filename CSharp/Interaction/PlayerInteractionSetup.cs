using Godot;
using System;

namespace Trakction.Interaction;

/// <summary>
/// Sets up interaction rays for both Desktop (Camera) and VR (Hands).
/// Attach this to the Player root (CharacterBody3D).
/// </summary>
public partial class PlayerInteractionSetup : Node
{
    [Export] public NodePath CameraPath { get; set; } = "XROrigin3D/XRCamera3D";
    [Export] public NodePath LeftHandPath { get; set; } = "XROrigin3D/LeftHand";
    [Export] public NodePath RightHandPath { get; set; } = "XROrigin3D/RightHand";

    [Export] public string DesktopInteractAction { get; set; } = "ui_accept";
    [Export] public string VRLeftInteractAction { get; set; } = "trigger_click"; // Default OpenXR action name often used
    [Export] public string VRRightInteractAction { get; set; } = "trigger_click";

    public override void _Ready()
    {
        // Defer setup to ensure children are ready
        CallDeferred(nameof(SetupInteraction));
    }

    private void SetupInteraction()
    {
        SetupDesktopInteraction();
        SetupVRInteraction();
    }

    private void SetupDesktopInteraction()
    {
        var camera = GetNodeOrNull<Camera3D>(CameraPath);
        if (camera != null)
        {
            // Check if already exists
            if (camera.HasNode("DesktopInteractor")) return;

            var interactor = new InteractorRay3D();
            interactor.Name = "DesktopInteractor";
            interactor.InteractAction = DesktopInteractAction;
            interactor.MaxDistance = 3.0f;
            // For desktop, we might want a crosshair or cursor, but the ray is invisible by default.
            camera.AddChild(interactor);
            
            // Ensure the ray points forward (-Z)
            interactor.Rotation = Vector3.Zero;
        }
        else
        {
            GD.PrintErr("PlayerInteractionSetup: Camera not found at " + CameraPath);
        }
    }

    private void SetupVRInteraction()
    {
        // Setup Left Hand
        SetupHand(LeftHandPath, VRLeftInteractAction, "LeftHandInteractor");
        
        // Setup Right Hand
        SetupHand(RightHandPath, VRRightInteractAction, "RightHandInteractor");
    }

    private void SetupHand(NodePath handPath, string actionName, string name)
    {
        var hand = GetNodeOrNull<Node3D>(handPath);
        if (hand != null)
        {
            if (hand.HasNode(name)) return;

            var interactor = new InteractorRay3D();
            interactor.Name = name;
            interactor.VRButtonName = actionName;
            interactor.MaxDistance = 2.0f; // Shorter reach for VR usually
            hand.AddChild(interactor);
            
            // Ensure the ray points forward (-Z) relative to the controller
            interactor.Rotation = Vector3.Zero;
        }
    }
}

