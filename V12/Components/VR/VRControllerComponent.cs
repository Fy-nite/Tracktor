using Godot;

namespace V12.Components.VR
{
    public enum VRHand
    {
        Left,
        Right
    }

    public class VRControllerComponent : Component
    {
        public override string Category => "VR";

        [InspectorExpose]
        public bool Hand { get; set; } = true; // true for Left, false for Right

        public VRControllerComponent() : base("VR Controller", "Creates an XR tracked controller")
        {
        }
        public override void OnAttach() {}

        public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);

        public override Node3D RunComponentLogic()
        {
            var cHand = new XRController3D();
            cHand.Tracker = new StringName(Hand ? "left_hand" : "right_hand");
            return cHand;
        }
    }
}
