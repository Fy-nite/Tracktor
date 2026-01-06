using Godot;
using V12.Components.VR;
using V12.Elements;
using V12.Interfaces;
using V12.Types;

namespace V12.Components.Tools
{
    public class ToolComponent : NodeComponent, ITool
    {
        public bool IsEquipped { get; private set; }
        public Element? PrevOwner { get; private set;  }
        public Element? Owner { get; private set; }

        public event Action<ITool, Element?>? ToolEquipped;
        public event Action<ITool, Element?>? ToolUnequipped;
        /// <summary>
        /// Equips the tool to the specified owner, attaching it to the owner's scene element and updating ownership
        /// state.  
        /// </summary>
        /// <remarks>If the tool is already equipped, calling this method will reassign it to the new
        /// owner. The method also triggers the ToolEquipped event after the tool is attached.</remarks>
        /// <param name="owner">The Element that will become the new owner of the tool. Cannot be null.</param>
        public virtual void Equip(Element owner)
        {
            IsEquipped = true;
            PrevOwner = this.attachedElement as Element;
            Owner = owner;

            // Update ECS/world model
            this.attachedElement = owner.ParentElement;

            // Attach the tool's scene node to the owner's scene node
            var toolNode = this.sceneElement;
            var ownerNode = owner.sceneElement;
            if (toolNode != null && ownerNode != null && toolNode.GetParent() != ownerNode)
            {
                toolNode.GetParent()?.RemoveChild(toolNode);
                ownerNode.AddChild(toolNode);
            }

            ToolEquipped?.Invoke(this, owner);
        }


        /// <summary>
        /// Removes the tool from its current owner and updates its state to reflect that it is no longer equipped. 
        /// </summary>
        /// <remarks>After calling this method, the tool's ownership is cleared and its scene element is
        /// reattached to the world root. The ToolUnequipped event is raised to notify listeners of the unequip action.
        /// This method has no effect if the tool is already unequipped.</remarks>
        public virtual void Unequip()
        {
            IsEquipped = false;
            PrevOwner = Owner;
            Owner = null;


            var toolNode = this.sceneElement;
            var worldRoot = V12.WorldManager.RootElement?.sceneElement;
            if (toolNode != null && worldRoot != null && toolNode.GetParent() != worldRoot)
            {
                toolNode.GetParent()?.RemoveChild(toolNode);
                worldRoot.AddChild(toolNode);
            }

            // Update ECS/world model
            this.attachedElement = V12.WorldManager.RootElement;

            ToolUnequipped?.Invoke(this, PrevOwner);
        }

        public virtual void TriggerAction(string action, object? data = null)
        {
            // This method can be used to trigger local logic, but does not fire an event anymore.
        }

        public void RegisterActionEvent<E>(string eventName, Action<E> callback) where E : IEvent
        {
            // Register for an event using the EventManager
            V12.WorldManager.RootElement?.eventManager.registerEvent<E>(eventName, (evt) => callback((E)evt), this);
        }
        public Element GetParent()
        { 
            return this.attachedElement as Element;
        }
        public virtual void SetParent(Element parent) {
            this.attachedElement = parent;
        }
    }
}
