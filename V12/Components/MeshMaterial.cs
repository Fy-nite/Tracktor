using Godot;
using V12.Interfaces;

namespace V12.Components
{
    public class MeshMaterial : Component, IMaterial
    {
        public override string Category => "Materials";

        public Color AlbedoColor { get; set; }
        public string TexturePath { get; set; }
        public Material material { get; set; }
        public MeshMaterial() : base("Mesh Material", "Defines a material for meshes")
        {
        }
        public override void OnAttach() {}
        public MeshMaterial(Color albedoColor, string texturePath)
            : this()
        {
            ((IMaterial)this).AlbedoColor = albedoColor;
            ((IMaterial)this).TexturePath = texturePath;
        }
        public MeshMaterial(Color albedoColor, Material material)
            : this()
        {
            ((IMaterial)this).AlbedoColor = albedoColor;
            ((IMaterial)this).material = material;
        }
        public MeshMaterial(Material material)
            : this()
        {
            ((IMaterial)this).material = material;
        }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override Node3D RunComponentLogic()
        {
            return null; // This component does not create a Node3D directly
        }

        void IMaterial.ApplyToMesh(MeshInstance3D mesh)
        {
            mesh.MaterialOverride = this.material;
        }
        public MeshMaterial GetMaterial()
        {
            return this;
        }
        bool IEquatable<IMaterial>.Equals(IMaterial? other)
        {
            if (other == null) return false;
            return this.material == other.material;
        }
    }
}
