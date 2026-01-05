using Godot;

namespace V12.Components
{
    public class SubViewport : NodeComponent
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
        public SubViewport()
        {
            // defer creation of sceneElement until RunComponentLogic so object initializer values are applied
        }
        public SubViewport(Vector3 position, Vector3 rotation, Vector3 scale)
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

                var node3D = new Node3D
                {
                    Name = "SubViewportContainer"
                };
                var subViewport = new Godot.SubViewport
                {
                    Name = "SubViewport"
                };
                node3D.AddChild(subViewport);
                sceneElement = node3D;
            }
        }
    }
}
