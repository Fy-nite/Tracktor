using System;
using V12.Interfaces;

namespace V12.Elements;

public class EventManager
{
    Dictionary<string,List<Tuple<IEvent, Action>>> callbacks;
    public EventManager()
    {
        callbacks = new Dictionary<string,List<Tuple<IEvent, Action>>>();
    }
}
