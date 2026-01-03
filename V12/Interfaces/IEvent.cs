using System;

namespace V12.Interfaces;

public interface IEvent
{
    public void fireEvent(Action<IEvent> callback)
    {
        callback(this);
    }
}
