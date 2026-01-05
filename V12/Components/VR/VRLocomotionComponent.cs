using Godot;

namespace V12.Components.VR
{
    public class VRLocomotionComponent : Component
    {
        public override string Category => "VR";

        [InspectorExpose]
        public float MoveSpeed { get; set; } = 2.5f;

        [InspectorExpose]
        public float TurnSpeedDegrees { get; set; } = 90f;

        public override string Description => "Provides basic locomotion settings";

        public VRLocomotionComponent()
        {
        }
        public override void OnAttach() { }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }
    }
}
