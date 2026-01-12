using Godot;

using V12.Interfaces;

namespace V12.Extensions
{
    public static class WorldExtensions
    {
        // Walk up parent element chain to find the root element
        public static IWorldElement GetRootElement(this IWorldElement element)
        {
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

        // Convenience overload using the globally registered current syncer
        public static Node? GetSceneNode(this IComponent component)
        {
            if (component is NodeComponent nodeComponent)
            {
                return nodeComponent.sceneElement;
            }
            return null;
        }
    }
}
