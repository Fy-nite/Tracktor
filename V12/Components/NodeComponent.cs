using Godot;
using V12.Interfaces;

namespace V12.Components
{
    public class NodeComponent : Component, IExist
    {
        private Node3D? _sceneElement;
        public Node3D? sceneElement
        {
            get => _sceneElement;
            set
            {
                _sceneElement = value;
                // keep base protected field in sync for general components
                _sceneNode = value;
            }
        }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override Node? RunComponentLogic()
        {
            // By default return the cached scene node (if any). Specific components
            // should create and assign their node in OnAttach().
            return _sceneNode;
        }

        public override void OnAttach()
        {
            // When attached, try to parent the scene element under the element's scene node
            if (sceneElement == null) return;
            var parentNode = attachedElement?.sceneElement as Node3D;
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