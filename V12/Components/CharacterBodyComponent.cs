using Godot;

namespace V12.Components
{
    public class CharacterBodyComponent : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }

        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = Vector3.One;

        [InspectorExpose]
        public float FloorSnapLength { get; set; } = 0.5f;

        public override string Category => "Physics";
        public override string Description => "Wraps a Godot CharacterBody3D node.";

        public CharacterBodyComponent()
        {
            Name = "CharacterBody3D";
        }

        public override void OnAwake()
        {
            if (sceneElement != null)
            {
                return;
            }

            sceneElement = new CharacterBody3D
            {
                Name = string.IsNullOrWhiteSpace(Name) ? "CharacterBody3D" : Name,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                FloorSnapLength = FloorSnapLength
            };
        }
    }
}
