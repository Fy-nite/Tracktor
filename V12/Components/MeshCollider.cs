using Godot;
using System.Collections.Generic;
using V12.Types;

namespace V12.Components
{
    internal class MeshCollider : Component
    {
        public override string Category => "Colliders";

        public List<DoubleVector3> Vertices { get; set; } = new();
        public List<int> Indices { get; set; } = new();

        public override void OnAttach() { }

        public void AttachMeshData(List<DoubleVector3> vertices, List<int> indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        public override Control GenerateUI()
        {
            throw new NotImplementedException();
        }
    }
}
