using Godot;
using V12.Components;

namespace V12.Components.VR
{
    public class VRRigComponent : Component
    {
        public override string Category => "VR";

        public float PlayerHeight { get; set; } = 1.7f;

        public VRRigComponent() : base("VR Rig", "Creates an XR rig (origin + camera + controllers)")
        {
        }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override Node3D RunComponentLogic()
        {
            new XRInit()._Ready();
            return new Node3D();
        }
    }
}
