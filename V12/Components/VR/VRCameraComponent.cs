using Godot;

namespace V12.Components.VR
{
	public class VRCameraComponent : NodeComponent
	{
		public override string Category => "VR";

		[InspectorExpose]
		public float HeightOffset { get; set; } = 1.7f;

        public override string? Description => "Creates an XR camera for head tracking";

		public VRCameraComponent()
        {
            sceneElement = new XRCamera3D
            {
                Name = "VRCamera"
            };
        }
		public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);
	}
}
