using Godot;

namespace V12.Components
{
    public class RayCastComponent : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }

        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = Vector3.One;

        [InspectorExpose]
        public Vector3 TargetPosition { get; set; } = new Vector3(0, 0, -1);

        [InspectorExpose]
        public bool Enabled { get; set; } = true;

        [InspectorExpose]
        public bool CollideWithAreas { get; set; } = true;

        [InspectorExpose]
        public bool CollideWithBodies { get; set; } = true;

        public override string Category => "Physics";
        public override string Description => "Wraps a Godot RayCast3D node.";

        public RayCastComponent()
        {
            Name = "RayCast3D";
        }

        public override void OnAwake()
        {
            if (sceneElement != null)
            {
                return;
            }

            sceneElement = new RayCast3D
            {
                Name = string.IsNullOrWhiteSpace(Name) ? "RayCast3D" : Name,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                TargetPosition = TargetPosition,
                Enabled = Enabled,
                CollideWithAreas = CollideWithAreas,
                CollideWithBodies = CollideWithBodies
            };
        }
    }
}
