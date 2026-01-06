using Godot;
using V12.Components.VR;
using V12.Elements;
using V12.Interfaces;

namespace V12.Components.Tools
{
    public class ToolComponent : NodeComponent, ITool
    {
        public bool IsEquipped { get; private set; }
        public NodeComponent? Owner { get; private set; }

        public virtual void Equip(NodeComponent owner)
        {
            IsEquipped = true;
            Owner = owner;
        }

        public virtual void Unequip()
        {
            IsEquipped = false;
            Owner = null;
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
    }
}
