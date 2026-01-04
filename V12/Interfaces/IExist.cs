using System;
using Godot;
using V12.Elements;

namespace V12.Interfaces;

public interface IExist
{
    public Node3D? sceneElement { get; set; }
}
