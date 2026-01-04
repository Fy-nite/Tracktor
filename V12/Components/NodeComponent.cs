using Godot;
using V12.Interfaces;

namespace V12.Components
{
    public class NodeComponent : Component, IExist
    {
        public Node3D sceneElement { get; set; }

        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }
		public override void OnAttach()
        {
            attachedElement?.sceneElement.AddChild(sceneElement);
        }
    }
}