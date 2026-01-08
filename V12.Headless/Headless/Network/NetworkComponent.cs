using Godot;
using System.Collections.Generic;
using V12.Interfaces;
using V12.Types;

namespace V12.Headless.Network
{
    public class NetworkComponent : IComponent
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool active { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<IComponent> Addons { get; set; } = new();
        public IWorldElement? attachedElement { get; set; }

        public void AddChildAddon(IComponent addon)
        {
            Addons.Add(addon);
        }

        public Control GenerateUI()
        {
            return new Control();
        }

        public void MergeFrom(NetworkComponent other)
        {
            Description = other.Description;
            Category = other.Category;
            Name = other.Name;
        }

        public void OnAwake() { }
        public void OnSleep() { }
        public void OnAttach() { }
        public void OnDetach() { }
        public void OnUpdate(double delta) { }
        public void OnDestroy() { }

        public void RemoveChildAddon(IComponent addon)
        {
            Addons.Remove(addon);
        }
    }
}
