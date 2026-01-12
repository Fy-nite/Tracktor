using Godot;
using V12.Elements;
using V12.Interfaces;

namespace V12.Components.Tools
{
    public class DevTipComponent : NodeComponent
    {
        [InspectorExpose]
        public string TipText { get; set; } = "";

        public DevTipComponent() { }
        public DevTipComponent(string tip)
        {
            TipText = tip;
        }

        public void ShowTip()
        {
            GD.Print($"[DevTip] {TipText}");
            // You can extend this to show a UI popup or overlay
        }
        
        public void RegisterToolUseTip()
        {
            Tracktor.Tracktor.Game.CSharp.World.WorldManager.RootElement?.eventManager.registerEvent<IEvent>("Tool_Use", (evt) => ShowTip(), this);
        }
    }
}
