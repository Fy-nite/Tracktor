using System;
using Godot;

namespace Tracktor.Components
{
    /// <summary>
    /// Fallback component that instantiates a Godot Node3D by type name when no specific V12 component exists.
    /// Useful for generated scenes where an exact component mapping is unavailable.
    /// </summary>
    public class GenericNodeComponent : NodeComponent
    {
        [InspectorExpose]
        public string NodeTypeName { get; set; } = "Godot.Node3D";

        [InspectorExpose]
        public Vector3 Position { get; set; }

        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = Vector3.One;

        public override string Category => "Generated";
        public override string Description => "Fallback node generated from a scene export.";

        public override void OnAwake()
        {
            if (sceneElement != null)
            {
                return;
            }

            Node3D? created = null;
            try
            {
                var type = Type.GetType(NodeTypeName);
                if (type != null && typeof(Node3D).IsAssignableFrom(type))
                {
                    created = Activator.CreateInstance(type) as Node3D;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[GenericNodeComponent] Failed to create '{NodeTypeName}': {ex.Message}");
            }

            sceneElement = created ?? new Node3D();
            sceneElement.Name = string.IsNullOrWhiteSpace(Name) ? sceneElement.Name : Name;
            sceneElement.Position = Position;
            sceneElement.Rotation = Rotation;
            sceneElement.Scale = Scale;
        }
    }
}
