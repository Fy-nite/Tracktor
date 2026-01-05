using Godot;
using V12.Types;

namespace V12.Components.Test
{
    public class EventSender : Component
    {
        public int Interval = 2;
        private double time = 0;
        public string Event = "";
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnUpdate(double delta)
        {
            time += delta;
            if (time > Interval)
            {
                time = 0;
                attachedElement?.eventManager.fireEvent(new GenericEvent(), Event);
            }
            base.OnUpdate(delta);
        }
    }
}
