using Godot;
using V12.Interfaces;

namespace V12.Extensions
{
    public static class WorldExtensions
    {
        // Walk up parent element chain to find the root element
        public static IWorldElement GetRootElement(this IWorldElement element)
        {
            if (element == null) return null;
            var current = element;
            while (current.ParentElement != null)
            {
                current = current.ParentElement;
            }
            return current;
        }

        // Get sibling components on the same element
        public static List<IComponent> GetSiblingComponents(this IComponent component)
        {
            if (component == null || component.attachedElement == null) return new List<IComponent>();
            return component.attachedElement.Components.Where(c => c != component).ToList();
        }

        // Find first component of given type in this element or any ancestor
        public static T? FindComponentInAncestors<T>(this IComponent component) where T : class, IComponent
        {
            if (component == null) return null;
            IWorldElement? current = component.attachedElement;
            while (current != null)
            {
                var found = current.Components.OfType<T>().FirstOrDefault();
                if (found != null) return found as T;
                current = current.ParentElement;
            }
            return null;
        }

        // Return the Godot Node associated with this component if the world syncer is available
        // This requires the caller to have access to the WorldElementSceneSyncer instance.
        public static Node GetSceneNode(this IComponent component, WorldElementSceneSyncer syncer)
        {
            if (component == null || syncer == null) return null;
            return syncer.GetNodeForComponent(component);
        }

        // Convenience overload using the globally registered current syncer
        public static Node GetSceneNode(this IComponent component)
        {
            if (component == null) return null;
            var syncer = V12.WorldManager.CurrentSyncer;
            if (syncer == null) return null;
            return syncer.GetNodeForComponent(component);
        }

        public static Node GetSceneNode(this IWorldElement element, WorldElementSceneSyncer syncer)
        {
            if (element == null || syncer == null) return null;
            return syncer.GetNodeForElement(element);
        }

        public static Node GetSceneNode(this IWorldElement element)
        {
            if (element == null) return null;
            var syncer = V12.WorldManager.CurrentSyncer;
            if (syncer == null) return null;
            return syncer.GetNodeForElement(element);
        }
    }
}
