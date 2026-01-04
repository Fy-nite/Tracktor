using Godot;

namespace V12.Components
{
    public class DesktopPlayer : NodeComponent
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
        public DesktopPlayer()
        {
            // defer creation of sceneElement until RunComponentLogic so object initializer values are applied
        }
        public DesktopPlayer(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAttach() { 
            if (sceneElement == null)
            {
                sceneElement = GD.Load<PackedScene>("res://V12/Scenes/DesktopPlayer.tscn").Instantiate<Node3D>();
            }
        }
    }
}
