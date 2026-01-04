using Godot;

namespace V12.Components
{
    public class Spotlight : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }
        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; }
        [InspectorExpose]
        public Color LightColor { get; set; } = Colors.White;
        [InspectorExpose]
        public Color ShadowColor { get; set; }
        [InspectorExpose]
        public float Energy { get; set; } = 1.0f;
        [InspectorExpose]
        public float Size { get; set; } = 45.0f;

        public override string Category => "Mesh";
        public override string Description => "A simple box mesh";

        public string MeshType => "Box";
        public Spotlight() { }
        public Spotlight(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAttach() 
        {
            if (sceneElement == null)
            {
                sceneElement = new SpotLight3D
                {
                    Position = Position,
                    Name = "Spotlight3D",
                    Rotation = Rotation,
                    Scale = Scale,
                    LightColor = LightColor,
                    SpotRange = Size,
                    LightEnergy = Energy,

                };
            }
        }
    }
}
