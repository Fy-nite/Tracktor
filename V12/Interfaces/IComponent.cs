using Godot;
using V12.Types;

namespace V12.Interfaces
{
    public interface IComponent
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        bool active { get; set; }
        // A human friendly name for the component (default implementations may return type name)
        public string Name { get; set; }
        public List<IComponent> Addons { get; set; }
        public IWorldElement? attachedElement { get; set; }

        // Ran when the component is created
        public void OnAwake();
        // Ran whenever the component goes to sleep / inactive
        public void OnSleep();
        // Ran whenever the component is attached to a element
        public void OnAttach();
        // Ran before the component is removed from a element (moved or deleted)
        public void OnDetach();
        // Ran once every engine update
        public void OnUpdate(double delta);
        // Ran whenever the component becomes active
        public void OnDestroy();

        // Add a child component
        public void AddChildAddon(IComponent addon);
        // Remove a child component
        public void RemoveChildAddon(IComponent addon);

        public Control GenerateUI();
    }
}
