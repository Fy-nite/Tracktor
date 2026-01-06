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
        public Mesh mesh;
        public BoxMesh()
        {
            Name = "Box";
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
                mesh = new Godot.BoxMesh
                {
                    Size = Scale
                };
                sceneElement = new MeshInstance3D
                {
                    Mesh = mesh,
                    Position = Position,
                    Rotation = Rotation,
                    Name = Name,
                    
                };
                var CSGCollider = new CollisionShape3D
                {
                    Shape = new BoxShape3D
                    {
                        Size = Scale
                    },
                    
                };
                var shape = new StaticBody3D();
                shape.AddChild(CSGCollider);
                sceneElement.AddChild(shape);
            }
        }
    }
}
