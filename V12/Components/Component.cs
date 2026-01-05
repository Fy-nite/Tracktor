using Godot;
using V12;
using V12.Interfaces;
using V12.Types;

namespace V12.Components
{
    public abstract class Component : IComponent
    {
        public virtual string? Description { get; set; }
        public virtual string? Category { get; set; }
        bool active { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<IComponent> Addons { get; set; } = new();
        public IWorldElement? attachedElement { get; set; }
        bool IComponent.active { get; set; }
        // Node associated with this component in the scene tree (if any)


        // Ran when the component is created
        public virtual void OnAwake() { }
        // Ran whenever the component goes to sleep / inactive
        public virtual void OnSleep() { }
        // Ran whenever the component is attached to a element (should register events here)
        public virtual void OnAttach() { }
        // Ran before the component is removed from a element (moved or deleted)
        public virtual void OnDetach()
        {
            attachedElement?.eventManager.deregisterComponentEvents(this);
        }
        // Ran once every engine update
        public virtual void OnUpdate(double delta) { }
        // Ran once when the component is about to be destroyed
        public virtual void OnDestroy() { }

        // Add a child component
        public virtual void AddChildAddon(IComponent addon)
        {
            Addons.Add(addon);
        }
        // Remove a child component
        public virtual void RemoveChildAddon(IComponent addon)
        {
            Addons.Remove(addon);
        }

        public abstract Control GenerateUI();
    }
}
