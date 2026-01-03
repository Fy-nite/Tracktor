using Godot;
using System;
using System.Collections.Generic;
using System.Text;

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
