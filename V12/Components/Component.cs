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
        public List<IComponent> SubComponents { get; set; } = new();
        public IWorldElement? attachedElement { get; set; }
        bool IComponent.active { get; set; }
        // Node associated with this component in the scene tree (if any)
        protected Node? _sceneNode;

        // Ran when the component is created
        public virtual void OnAwake() {}
        // Ran whenever the component is attached to a element (should register events here)
        public virtual void OnAttach() {}
        // Ran before the component is removed from a element (moved or deleted)
        public virtual void OnDetach()
        {
            attachedElement?.eventManager.deregisterComponentEvents(this);
        }
        // Ran once every engine update
        public virtual void OnUpdate() {}
        // Ran whenever the component becomes active
        public virtual void OnActivated() {}
        // Ran whenever the component becomes deactive
        public virtual void OnDeactivated() {}
        // Ran once when the component is about to be destroyed
        public virtual void OnDestroy() {}

        public virtual Node? RunComponentLogic()
        {
            // By default components don't produce nodes
            return null;
        }

        public virtual void OnSceneNodeAssigned(Node? node)
        {
            _sceneNode = node;
        }

        public virtual void OnSceneNodeRemoved()
        {
            _sceneNode = null;
        }

        public virtual void OnTick(float delta) { }

        // Add a child component
        public virtual void AddChildComponent(IComponent component)
        {
            SubComponents.Add(component);
        }
        // Remove a child component
        public virtual void RemoveChildComponent(IComponent component)
        {
            SubComponents.Remove(component);
        }

        public abstract Control GenerateUI();
    }
}
