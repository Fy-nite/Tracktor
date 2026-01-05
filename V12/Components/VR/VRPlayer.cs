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

        public override void OnAwake()
        {
            if (sceneElement == null)
            {
                sceneElement = GD.Load<PackedScene>("res://V12/Scenes/VR/VRPlayerBody.tscn").Instantiate<Node3D>();
                GD.Print("[VRPlayer] Created VRPlayer Node3D in OnAwake");
                // Connect XRController3D signals from LeftHand and RightHand to local handlers
                var leftNode = sceneElement.GetNodeOrNull("LeftHand");
                var rightNode = sceneElement.GetNodeOrNull("RightHand");

                if (leftNode is XRController3D leftHand)
                {
                    leftHand.Connect("button_pressed", new Callable(xrHandler, "OnLeftNodeButtonPressed"));
                    leftHand.Connect("input_float_changed", new Callable(xrHandler, "OnLeftNodeFloatChanged"));
                    leftHand.Connect("input_vector2_changed", new Callable(xrHandler, "OnLeftNodeVector2Changed"));
                    GD.Print("[VRPlayer] Connected LeftHand XRController3D signals");
                }

                if (rightNode is XRController3D rightHand)
                {
                    rightHand.Connect("button_pressed", new Callable(xrHandler, "OnRightNodeButtonPressed"));
                    rightHand.Connect("input_float_changed", new Callable(xrHandler, "OnRightNodeFloatChanged"));
                    rightHand.Connect("input_vector2_changed", new Callable(xrHandler, "OnRightNodeVector2Changed"));
                    GD.Print("[VRPlayer] Connected RightHand XRController3D signals");
                }

                // Subscribe to XRInteractionHandler events (centralized handling)
                xrHandler.ButtonPressed += (hand, button) =>
                {
                    GD.Print($"[VRPlayer] {hand} controller pressed {button}");
                    
                };
                xrHandler.TriggerChanged += (hand, value) =>
                {
                    GD.Print($"[VRPlayer] {hand} trigger value: {value}");
                };
                xrHandler.StickChanged += (hand, value) =>
                {
                    GD.Print($"[VRPlayer] {hand} stick value: {value}");
                };

                base.OnAwake();
                base.OnAttach();
            }
        }

    // NOTE: signal handlers moved into XRInteractionHandler and connected directly to it.
    }
}