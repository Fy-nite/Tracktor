using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Interfaces
{
    public interface IMaterial : IEquatable<IMaterial>
    {
        public Color AlbedoColor { get; set; }
        public string TexturePath { get; set; }
        public Material material { get; set; }
        void ApplyToMesh(MeshInstance3D mesh);
    }
}
