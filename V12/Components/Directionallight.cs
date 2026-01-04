using Godot;
using V12.Interfaces;

namespace V12.Components
{
    public class DirectionalLight : NodeComponent
    {
        [InspectorExpose]
        public float Energy { get; set; } = 1.0f;
        [InspectorExpose]
        public Color Color { get; set; } = new Color(1, 1, 1);
        [InspectorExpose]
        public bool ShadowEnabled { get; set; } = false;
        [InspectorExpose]
        public bool ShadowDisabled { get; set; }
        [InspectorExpose]
        public Vector3 Position { get; set; }
        [InspectorExpose]
        public Vector3 Direction { get; set; }
        [InspectorExpose]
        public Vector3 Rotation { get; set; }
        [InspectorExpose]
        public Vector3 Scale { get; set; } = new Vector3(1, 1, 1);
        [InspectorExpose]
        public string Name { get; set; } = "DirectionalLight";
        [InspectorExpose]
        public override string Description => "A directional light that simulates sunlight.";
        public override string Category => "Lights";
        public DirectionalLight()
        {
            // Defer creation so initializer values (Position, Rotation, etc.) are applied
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAttach()
        {
            if (sceneElement == null)
            {
                sceneElement = new DirectionalLight3D
                {
                    Position = Position,
                    Rotation = Rotation,
                    Scale = Scale,
                    Name = Name,
                    LightEnergy = Energy,
                    LightColor = Color,
                };
            }
        }
    }
}
