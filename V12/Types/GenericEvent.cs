using V12.Elements;
using V12.Interfaces;

namespace V12.Types
{
    public class GenericEvent : IEvent
    {
        public void fireEvent(IEventCallback c)
        {
            c.callback.Invoke(this);
        }
    }
}
