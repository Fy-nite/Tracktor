#nullable enable
using Godot;

namespace Trakction.PhysicalUI;

[Tool]
public partial class PhysicalControl3D : Node3D
{
    [Export] public bool Enabled { get; set; } = true;
}
