using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Interfaces
{
    public interface IComponent
    {
        string Name { get; }
        string Description { get; }
        public Control GenerateUI();
        public Node3D RunComponentLogic();
        public List<IComponent> SubComponents { get; set; }
        IWorldElement ParentElement { get; set; }
    }
}
