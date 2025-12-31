using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using V12.Interfaces;

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


        public override string Category => "Mesh";

        public string MeshType => "Box";
        public string Name => "Box Mesh";
        public string Description => "A simple box mesh.";
        public BoxMesh()
        {
            
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
            return new CsgBox3D
            {
                Position = Position,
                Rotation = Rotation,
                Scale = Scale,
                Material = this.ParentElement.Components.Find(c => c is MeshMaterial) is MeshMaterial matComp ? matComp.GetMaterial().material : null
            };
        }

        
    }
}
