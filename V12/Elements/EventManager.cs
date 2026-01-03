using System;
using Godot;
using V12.Interfaces;

namespace V12.Elements;

public class EventManager
{
    Dictionary<string,List<Tuple<Type, Action<IEvent>>>> callbacks;
    public EventManager()
    {
        callbacks = new Dictionary<string,List<Tuple<Type, Action<IEvent>>>>();
    }
    public void fireEvent(IEvent e, string eventName)
    {
        if (!callbacks.ContainsKey(eventName))
        {
            GD.Print($"[EventManager] fireEvent: tried to fire event {eventName} which has no listeners.");
            return;
        }
        foreach (Tuple<Type, Action<IEvent>> callback in callbacks[eventName])
        {
            if (callback.Item1.IsAssignableFrom(e.GetType()))
                e.fireEvent(callback.Item2);
        }
    }
    public void regsiterEvent<E>(string eventName, Action<IEvent> eventCallback) where E : IEvent
    {
        if(!callbacks.ContainsKey(eventName))
        {
            callbacks[eventName] = new();
        }
        callbacks[eventName].Add(new Tuple<Type, Action<IEvent>>(typeof(E), eventCallback));
    }
}
