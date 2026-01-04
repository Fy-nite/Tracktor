using Godot;

namespace V12.Components
{
    public class BoxMesh : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }
        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; }
        [InspectorExpose]
        public List<string> strings { get; set; } = new List<string>();

        public override string Category => "Mesh";
        public override string Description => "A simple box mesh";

        public string MeshType => "Box";
        public BoxMesh()
        {
            // defer creation of sceneElement until RunComponentLogic so object initializer values are applied
        }
        public BoxMesh(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAwake() 
        {
            if (sceneElement == null)
            {
                sceneElement = new CsgBox3D
                {
                    Position = Position,
                    Name = "BoxMesh",
                    Rotation = Rotation,
                    Scale = Scale,
                    UseCollision = true
                };
            }
        }
    }
}
