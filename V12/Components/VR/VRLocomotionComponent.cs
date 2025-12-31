using Godot;
using V12.Components;

namespace V12.Components.VR
{
    public class VRLocomotionComponent : Component
    {
        public override string Category => "VR";

        [InspectorExpose]
        public float MoveSpeed { get; set; } = 2.5f;

        [InspectorExpose]
        public float TurnSpeedDegrees { get; set; } = 90f;

        public VRLocomotionComponent() : base("VR Locomotion", "Provides basic locomotion settings")
        {
        }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override Node3D RunComponentLogic() => new Node3D
        {
            Name = Name
        };
    }
}
