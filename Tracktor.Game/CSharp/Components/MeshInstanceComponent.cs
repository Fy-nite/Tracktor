using System;
using Godot;

namespace Tracktor.Components
{
    public class MeshInstanceComponent : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }

        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = Vector3.One;

        [InspectorExpose]
        public string? MeshResourcePath { get; set; }

        [InspectorExpose]
        public string? MeshTypeName { get; set; }

        public override string Category => "Mesh";
        public override string Description => "Wraps a Godot MeshInstance3D node.";

        public MeshInstanceComponent()
        {
            Name = "MeshInstance3D";
        }

        public override void OnAwake()
        {
            if (sceneElement != null)
            {
                return;
            }

            var meshInstance = new MeshInstance3D
            {
                Name = string.IsNullOrWhiteSpace(Name) ? "MeshInstance3D" : Name,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                Mesh = ResolveMesh()
            };

            sceneElement = meshInstance;
        }

        private Mesh ResolveMesh()
        {
            Mesh? mesh = null;

            if (!string.IsNullOrWhiteSpace(MeshResourcePath))
            {
                mesh = ResourceLoader.Load<Mesh>(MeshResourcePath);
            }

            if (mesh == null && !string.IsNullOrWhiteSpace(MeshTypeName))
            {
                try
                {
                    var type = Type.GetType(MeshTypeName);
                    if (type != null && typeof(Mesh).IsAssignableFrom(type))
                    {
                        mesh = Activator.CreateInstance(type) as Mesh;
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[MeshInstanceComponent] Failed to create mesh '{MeshTypeName}': {ex.Message}");
                }
            }

            return mesh ?? new Godot.BoxMesh();
        }
    }
}
