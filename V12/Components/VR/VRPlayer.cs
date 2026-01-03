using Godot;

namespace V12.Components.VR
{
    public partial class VRPlayer : Node3D
    {
        public SubViewport GenerateVRPlayer()
        {
            var Player = GD.Load<PackedScene>("res://Scenes/VRPlayer.tscn").Instantiate<SubViewport>();
            return Player;
        }
    }
}
