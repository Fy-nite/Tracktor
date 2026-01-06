using V12.Types;

namespace V12.Components.Tools
{
    public interface ITool
    {
        bool IsEquipped { get; }
        Element? Owner { get; }
        Element? PrevOwner { get; }
        void Equip(Element owner);
        void Unequip();
        void TriggerAction(string action, object? data = null);

        // Optional: Events for equip/unequip
        event Action<ITool, Element?>? ToolEquipped;
        event Action<ITool, Element?>? ToolUnequipped;
    }
}
