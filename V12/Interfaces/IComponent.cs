using Godot;
using V12;

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

        // Convenience helpers with default interface implementations so existing
        // components automatically gain these helpers.
        public IWorldElement GetParentElement()
        {
            return ParentElement;
        }

        public IWorldElement GetRootElement()
        {
            var current = ParentElement;
            while (current?.ParentElement != null)
            {
                current = current.ParentElement;
            }
            return current;
        }

        public List<IComponent> GetSiblingComponents()
        {
            if (ParentElement == null) return new List<IComponent>();
            return ParentElement.Components.Where(c => c != this).ToList();
        }

        public T? FindComponentInAncestors<T>() where T : class, IComponent
        {
            var current = ParentElement;
            while (current != null)
            {
                var found = current.Components.OfType<T>().FirstOrDefault();
                if (found != null) return found as T;
                current = current.ParentElement;
            }
            return null;
        }

        // Returns the Godot Node for this component using the provided syncer.
        public Node GetSceneNode(WorldElementSceneSyncer syncer)
        {
            if (syncer == null) return null;
            return syncer.GetNodeForComponent(this);
        }

        // Convenience overload that uses the globally registered current syncer.
        public Node GetSceneNode()
        {
            var syncer = WorldManager.CurrentSyncer;
            if (syncer == null) return null;
            return syncer.GetNodeForComponent(this);
        }

        // Called when the syncer has created or assigned a scene Node for this component.
        // Components can use this to capture a reference to their slot/node and
        // perform initialization that requires the Godot Node.
        public void OnSceneNodeAssigned(Node? node)
        {
            // default no-op
        }

        // Called when the syncer is about to remove the scene Node for this component.
        public void OnSceneNodeRemoved()
        {
            // default no-op
        }

        // Called each engine tick (if the syncer/engine calls it). Components that
        // need per-frame updates may override this.
        public void OnTick(float delta)
        {
            // default no-op
        }
    }
}
