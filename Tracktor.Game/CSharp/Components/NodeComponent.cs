using Godot;
using V12.Interfaces;

namespace Tracktor.Components
{
    public class NodeComponent : Component, IExist
    {
        public Node3D? sceneElement { get; set; }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }
        public override void OnAttach()
        {
            // When attached, try to parent the scene element under the element's scene node
            if (sceneElement == null) return;
            var parentNode = attachedElement?.sceneElement;
            if (parentNode != null)
            {
                // Remove from previous parent if necessary
                if (sceneElement.GetParent() != null)
                    sceneElement.GetParent().RemoveChild(sceneElement);
                parentNode.AddChild(sceneElement);
            }
        }
    }
}