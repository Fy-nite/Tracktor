using Godot;

namespace V12.Components.VR
{
    public class VROriginComponent : Component
    {
        public override string Category => "VR";

        public VROriginComponent() : base("VR Origin", "Provides an XR origin/root for the VR rig")
        {
        }

        public override Control GenerateUI() => ComponentUIGenerator.GenerateAutoUI(this);

        public override Node3D RunComponentLogic() => new XROrigin3D
        {
            Name = Name
        };
    }
}
