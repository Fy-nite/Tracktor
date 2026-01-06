namespace V12.Components.Tools
{
    public interface ITool
    {
        bool IsEquipped { get; }
        void Equip(NodeComponent owner);
        void Unequip();
        void TriggerAction(string action, object? data = null);
    }
}
