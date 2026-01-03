using Godot;

namespace V12.Interfaces
{
    public interface IMesh
    {
        string Name { get; }
        string Description { get; }
        string MeshType { get; }

        // Mesh data properties
        List<Vector3> Vertices { get; }
        List<Vector3> Normals { get; }
        List<Vector2> UVs { get; }
        List<int> Indices { get; }

        // Method to generate Godot Mesh
        Godot.Mesh GenerateGodotMesh();
    }
}
