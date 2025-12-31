using Godot;
using System.Collections.Generic;
using V12.Interfaces;

namespace V12.Components
{
    public abstract class Component : IComponent
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<IComponent> SubComponents { get; set; } = new();
        public IWorldElement ParentElement { get; set; }
        public abstract string Category { get; }

        public Component(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public Component()
        {
           
        }

        public abstract Control GenerateUI();
        public abstract Node3D RunComponentLogic();

        public void AddSubComponent(IComponent subComponent)
        {
            SubComponents.Add(subComponent);
        }

        public void RemoveSubComponent(IComponent subComponent)
        {
            SubComponents.Remove(subComponent);
        }
    }
}