using Godot;

namespace V12.Components.VR
{
    public class VRPlayer : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }
        [InspectorExpose]
        public Vector3 Rotation { get; set; }
        XRInteractionHandler xrHandler = new XRInteractionHandler();

        [InspectorExpose]
        public Vector3 Scale { get; set; }
        [InspectorExpose]
        public List<string> strings { get; set; } = new List<string>();

        public override string Category => "Mesh";
        public override string Description => "A simple box mesh";

        public string MeshType => "Box";
        public VRPlayer()
        {
            Name = "Player";
        }
        public VRPlayer(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }
        public override void OnUpdate(double delta)
        {
            base.OnUpdate(delta);

        }

        public static Node3D Generate()
        {
            Node3D VR = GD.Load<PackedScene>("res://V12/Scenes/VR/VRPlayerBody.tscn").Instantiate<Node3D>();
            GD.Print("[VRPlayer] Created VRPlayer Node3D in OnAwake");
            // Connect XRController3D signals from LeftHand and RightHand to local handlers
            var left = VR.GetNodeOrNull("LeftHand");
            var right = VR.GetNodeOrNull("RightHand");


            if (left is XRController3D leftHand)
            {
                leftHand.ButtonPressed += (button) =>
                {
                    GD.Print($"[VRPlayer] LeftHand button {button} pressed");
                    var ray = leftHand.FindChild("RightLazer") as RayCast3D;
                    // Raycast from left hand
                    if (ray != null) {
                        GodotObject contents = ray.GetCollider();
                        if (contents is Node3D targetNode) {
                            Vector3 hitPos = ray.GetGlobalTransform().Origin;
                            var pe = new V12.Elements.PointerEvent(V12.Elements.PointerEvent.Kind.Pressed, leftHand, targetNode, hitPos, hitPos);
                            var root = V12.WorldManager.RootElement;
                            if (root != null)
                                root.eventManager.fireEvent(pe, "pointer");
                        }
                        else if (contents != null) {
                            GD.Print($"[VRPlayer] LeftHand raycast hit non-Node3D collider: {contents}");
                        }
                        else {
                            GD.Print("[VRPlayer] LeftHand raycast did not hit");
                        }
                    }
                  
                };
                GD.Print("[VRPlayer] Connected LeftHand XRController3D signals");
            }

            if (right is XRController3D rightHand)
            {
                rightHand.ButtonPressed += (button) =>
                {
                    GD.Print($"[VRPlayer] RightHand button {button} pressed");
                    // Raycast from right hand
                    var from = rightHand.GlobalTransform.Origin;
                    var dir = -rightHand.GlobalTransform.Basis.Z;
                    float maxDist = 20f;
                    var to = from + dir * maxDist;
                    var space = rightHand.GetWorld3D().DirectSpaceState;
                    var exclude = new Godot.Collections.Array { rightHand };
                    
                    //var pe = new V12.Elements.PointerEvent(V12.Elements.PointerEvent.Kind.Pressed, rightHand, target, hitPos, hitPos);
                    var root = V12.WorldManager.RootElement;
                    if (root != null) { }
                        //root.eventManager.fireEvent(pe, "pointer");
                };
                GD.Print("[VRPlayer] Connected RightHand XRController3D signals");
            }
            return VR;

        }
    }
}