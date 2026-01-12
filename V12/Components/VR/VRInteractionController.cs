using Godot;
using System;

namespace V12.Components.VR
{
    [Obsolete("VRInteractionController does not match XR scheme, Use V12.Input.XRControllerInputHandler instead")]
    public class VRInteractionController
    {
        XRController3D leftHand;
        XRController3D rightHand;
        public VRInteractionController(Godot.SubViewport vrPlayerRoot)
        {
            var hands_root = vrPlayerRoot.GetChild(0);
            var leftHand = hands_root.FindChild("LeftHand") as XRController3D;
            var rightHand = hands_root.FindChild("RightHand") as XRController3D;
            if (leftHand == null || rightHand == null) { throw new Exception("Failed to find hands"); }
            this.leftHand = leftHand; this.rightHand = rightHand;
            leftHand.ButtonPressed += (b) => { GD.Print($"left: {b}"); };
            rightHand.ButtonPressed += (b) => { GD.Print($"right: {b}"); };
            leftHand.InputFloatChanged += (b, f) => { GD.Print($"left: {b} {f}"); };
            rightHand.InputFloatChanged += (b, f) => { GD.Print($"right: {b} {f}"); };
            leftHand.InputVector2Changed += (b, v) => { GD.Print($"left: {b} {v}"); };
            rightHand.InputVector2Changed += (b, v) => { GD.Print($"right: {b} {v}"); };
        }
    }
}
