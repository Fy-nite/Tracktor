using Godot;

namespace V12.Components.VR
{
    public enum VRHand
    {
        Left,
        Right
    }

    public class VRControllerComponent : NodeComponent
    {
        public override string Category => "VR";

        [InspectorExpose]
        public bool Hand { get; set; } = true; // true for Left, false for Right

        public override string Description => "Creates an XR tracked controller";

        public VRControllerComponent()
        {
            var cHand = new XRController3D();
            cHand.Tracker = new StringName(Hand ? "left_hand" : "right_hand");
            sceneElement = cHand;
        }
        public override void OnAttach() { }

        public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);
    }
}
