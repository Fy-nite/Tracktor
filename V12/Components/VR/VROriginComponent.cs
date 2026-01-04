using Godot;

namespace V12.Components.VR
{
    public class VROriginComponent : NodeComponent
    {
        public override string Category => "VR";
        public override string Description =>  "Provides an XR origin/root for the VR rig";
        public VROriginComponent()
        {
            sceneElement = new XROrigin3D
            {
                Name = "VR Origin"
            };
        }
        public override void OnAttach() {}

        public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);
    }
}
