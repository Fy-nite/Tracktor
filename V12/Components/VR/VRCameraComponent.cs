using Godot;
using V12.Components;

namespace V12.Components.VR
{
    public class VRCameraComponent : Component
    {
        public override string Category => "VR";

        [InspectorExpose]
        public float HeightOffset { get; set; } = 1.7f;

        public VRCameraComponent() : base("VR Camera", "Creates an XR camera for head tracking")
        {
        }

        public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);

        public override Node3D RunComponentLogic() => new XRCamera3D
        {
            Name = Name
        };
    }
}
