using System;
using Godot;

namespace Tracktor.Components
{
    public class CollisionShapeComponent : NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; }

        [InspectorExpose]
        public Vector3 Rotation { get; set; }

        [InspectorExpose]
        public Vector3 Scale { get; set; } = Vector3.One;

        [InspectorExpose]
        public string? ShapeResourcePath { get; set; }

        [InspectorExpose]
        public string ShapeTypeName { get; set; } = typeof(BoxShape3D).FullName + ", " + typeof(BoxShape3D).Assembly.GetName().Name;

        public override string Category => "Physics";
        public override string Description => "Wraps a Godot CollisionShape3D node.";

        public CollisionShapeComponent()
        {
            Name = "CollisionShape3D";
        }

        public override void OnAwake()
        {
            if (sceneElement != null)
            {
                return;
            }

            var collisionShape = new CollisionShape3D
            {
                Name = string.IsNullOrWhiteSpace(Name) ? "CollisionShape3D" : Name,
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                Shape = ResolveShape()
            };

            sceneElement = collisionShape;
        }

        private Shape3D ResolveShape()
        {
            Shape3D? shape = null;

            if (!string.IsNullOrWhiteSpace(ShapeResourcePath))
            {
                shape = ResourceLoader.Load<Shape3D>(ShapeResourcePath);
            }

            if (shape == null && !string.IsNullOrWhiteSpace(ShapeTypeName))
            {
                try
                {
                    var type = Type.GetType(ShapeTypeName);
                    if (type != null && typeof(Shape3D).IsAssignableFrom(type))
                    {
                        shape = Activator.CreateInstance(type) as Shape3D;
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[CollisionShapeComponent] Failed to create shape '{ShapeTypeName}': {ex.Message}");
                }
            }

            return shape ?? new BoxShape3D();
        }
    }
}
