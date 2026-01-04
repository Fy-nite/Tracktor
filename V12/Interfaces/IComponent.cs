using Godot;
using V12.Types;

namespace V12.Interfaces
{
    public interface IComponent
    {
        public string? Description { get; set; }
        public string? Category { get; set; }
        bool active { get; set; }
        public List<IComponent> SubComponents { get; set; }
        public IWorldElement? attachedElement { get; set; }
        // Ran when the component is created
        public void OnAwake();
        // Ran whenever the component is attached to a element
        public void OnAttach();
        // Ran before the component is removed from a element (moved or deleted)
        public void OnDetach();
        // Ran once every engine update
        public void OnUpdate();
        // Ran whenever the component becomes active
        public void OnActivated();
        // Ran whenever the component becomes deactive
        public void OnDeactivated();
        // Ran once when the component is about to be destroyed
        public void OnDestroy();

        // Add a child component
        public void AddChildComponent(IComponent component);
        // Remove a child component
        public void RemoveChildComponent(IComponent component);
        
        public Control GenerateUI();
    }
}
