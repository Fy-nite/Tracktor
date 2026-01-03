using System;
using System.Collections.Generic;
using System.Text;
using Godot;
namespace V12.Components.Android
{
    public class AndroidPlayer
    {
        public AndroidPlayer() { }
        public Godot.Node3D GenerateAndroidPlayer()
        {
            GD.Print("Generating Android Player");
            var androidPlayer = GD.Load<PackedScene>("res://Scenes/AndroidPlayer.tscn").Instantiate<Node3D>();
            return androidPlayer;
        }
    }
}
