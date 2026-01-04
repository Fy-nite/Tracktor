using Godot;

namespace V12.Components
{
    public class OmniLight : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }
        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = new Vector3(1,1,1);
        [InspectorExpose]
        public Color LightColor { get; set; } = Colors.White;
        [InspectorExpose]
        public float Energy { get; set; } = 1.0f;
        [InspectorExpose]
        public float Range { get; set; } = 10.0f;

        public override string Category => "Lighting";
        public override string Description => "An omni/point light";

        public OmniLight() {}

        public OmniLight(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAttach() {}

        public override Node? RunComponentLogic()
        {
            if (sceneElement == null)
            {
                sceneElement = new OmniLight3D
                {
                    Position = Position,
                    Name = "OmniLight3D",
                    Rotation = Rotation,
                    Scale = Scale,
                    LightColor = LightColor,
                    LightEnergy = Energy,
                };
                // Range property isn't on OmniLight3D in Godot 4's API directly; use shadow or attenuation via light size.
                if (sceneElement is OmniLight3D omni)
                {
                    omni.Range = Range;
                }
            }
            return sceneElement;
        }
    }
}
