using Godot;

namespace V12.Components.VR
{
    public partial class VRPlayer
    {
        public Godot.SubViewport GenerateVRPlayer()
        {
            var Player = GD.Load<PackedScene>("res://Scenes/VRPlayer.tscn").Instantiate<Godot.SubViewport>();
            
            return Player;
        }
    }
}
