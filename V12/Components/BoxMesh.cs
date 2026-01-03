using Godot;

namespace V12.Components
{
    public class BoxMesh : Component
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

        public string MeshType => "Box";
        public BoxMesh()
        {
            // Ensure we set the base Component properties instead of shadowing them
            Name = "BoxMesh";
            Description = "A simple box mesh.";
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

        public override Node3D RunComponentLogic()
        {
            var box = new CsgBox3D
            {
                Position = Position,
                Name = Name,
                Rotation = Rotation,
                Scale = Scale,
                UseCollision = true
                //Material = this.ParentElement.Components.Find(c => c is MeshMaterial) is MeshMaterial matComp ? matComp.GetMaterial().material : null
            };
            return box;
        }

        public override void OnAttach() {}
    }
}
