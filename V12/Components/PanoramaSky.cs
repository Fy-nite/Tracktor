using Godot;

namespace V12.Components
{
    public class PanoramaSky : NodeComponent
    {
        [InspectorExpose]
        public string TexturePath { get; set; } = string.Empty;

        [InspectorExpose]
        public Color Tint { get; set; } = Colors.Gray;

        [InspectorExpose]
        public float Size { get; set; } = 1000f;

        [InspectorExpose]
        public string Name { get; set; } = "PanoramaSky";

        public override string Category => "Environment";
        public override string Description => "A panorama sky using an inverted sphere and a texture.";

        public PanoramaSky() { }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAwake()
        {
            if (sceneElement == null)
            {
                var meshInstance = new MeshInstance3D();
                var sphere = new SphereMesh()
                {
                    Radius = 1.0f,
                    RadialSegments = 64,
                    Rings = 32
                };
                meshInstance.Mesh = sphere;
                meshInstance.Name = Name;
                meshInstance.Scale = new Vector3(Size, Size, Size); // invert normals so inside is visible

                var mat = new StandardMaterial3D();
                mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
                mat.CullMode = BaseMaterial3D.CullModeEnum.Front;
                mat.AlbedoColor = Tint;

                if (!string.IsNullOrWhiteSpace(TexturePath))
                {
                    // Try to load the texture resource; support common texture resource types
                    var res = ResourceLoader.Load(TexturePath);
                    if (res is Texture2D tex)
                    {
                        mat.AlbedoTexture = tex;
                    }
                    else
                    {
                        // If a file path was given (e.g., res:// or user path) but load failed, ignore silently
                    }
                }

                // Assign material to surface 0 if present
                meshInstance.SetSurfaceOverrideMaterial(0, mat);

                sceneElement = meshInstance;
            }
        }
    }
}
