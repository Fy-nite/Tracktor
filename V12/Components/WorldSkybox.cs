using Godot;

namespace V12.Components
{
    public class WorldSkybox : NodeComponent
    {
        [InspectorExpose]
        public Color SkyColor { get; set; } = new Color(0.2f, 0.5f, 0.9f);
        [InspectorExpose]
        public float Size { get; set; } = 1000f;

        public override string Category => "Environment";
        public override string Description => "A simple skybox using an inverted sphere mesh and basic material.";

        public WorldSkybox() { Name = "WorldSkybox"; }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAwake()
        {
            if (sceneElement == null)
            {
                // Create a MeshInstance3D with a SphereMesh, scale it large and invert normals by negative scale
                var meshInstance = new MeshInstance3D();
                var sphere = new SphereMesh()
                {
                    Radius = 1.0f,
                    Height = 0.0f,
                    RadialSegments = 32,
                    Rings = 16
                };
                meshInstance.Mesh = sphere;
                meshInstance.Name = Name;
                meshInstance.Scale = new Vector3(-Size, Size, Size); // negative X to invert normals

                var mat = new StandardMaterial3D();
                mat.AlbedoColor = SkyColor;
                mat.CullMode = BaseMaterial3D.CullModeEnum.Back;
                mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;

                meshInstance.SetSurfaceOverrideMaterial(0, mat);

                sceneElement = meshInstance;
            }
        }
    }
}
