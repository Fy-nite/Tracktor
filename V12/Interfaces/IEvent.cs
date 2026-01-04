using System;
using V12.Elements;

namespace V12.Interfaces;

public interface IEvent
{
    public void fireEvent(IEventCallback callback);
}
