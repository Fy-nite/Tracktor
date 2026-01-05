using System;
using Godot;
using V12.Interfaces;

namespace V12.Elements;

public abstract class IEventCallback
{
	public IComponent? component;
	public Action<IEvent> callback;
	public Type eventType;

	public IEventCallback(IComponent? component, Action<IEvent> callback, Type eventType)
	{
		this.component = component;
		this.callback = callback;
		this.eventType = eventType;
	}
}


public class EventCallback<E> : IEventCallback where E : IEvent
{
	public EventCallback(IComponent? component, Action<IEvent> callback) : base(component, callback, typeof(E)) { }
}

public class EventManager
{
	Dictionary<string, List<IEventCallback>> callbacks;
	IWorldElement? elem;
	public EventManager(IWorldElement? elem)
	{
		this.elem = elem;
		callbacks = new();
	}
	public void fireEvent(IEvent e, string eventName)
	{
		if (!callbacks.ContainsKey(eventName))
		{
			GD.Print($"[EventManager] fireEvent: tried to fire event {eventName} which has no listeners.");
			return;
		}
		foreach (var callback in callbacks[eventName])
		{
			if (callback.eventType.IsAssignableFrom(e.GetType()))
				e.fireEvent(callback);
		}
	}
	public void registerEvent<E>(string eventName, Action<IEvent> eventCallback, IComponent? registrator) where E : IEvent
	{
		GD.Print($"Registered event {eventName} on {elem?.sceneElement?.GetPath()}");
		if (!callbacks.ContainsKey(eventName))
		{
			callbacks[eventName] = new();
		}
		callbacks[eventName].Add(new EventCallback<E>(registrator, eventCallback));
	}
	public void deregisterEvent(string eventName, Action<IEvent> eventCallback)
	{
		if (!callbacks.ContainsKey(eventName) || !callbacks[eventName].Any(c => c.callback == eventCallback))
		{
			throw new Exception("Tried to deregister callback which is not registered");
		}
		callbacks[eventName].RemoveAll(c => c.callback == eventCallback);
	}
	public void deregisterComponentEvents(IComponent component)
	{
		foreach (var events in callbacks)
		{
			events.Value.RemoveAll(c => c.component == component);
		}
	}
	public void registerTreeEvent<E>(string eventName, Action<IEvent> eventCallback, IComponent? registrator, IWorldElement treeRoot) where E : IEvent
	{
		registerEvent<E>(eventName, eventCallback, registrator);
		foreach (var child in treeRoot.ChildElements)
		{
			treeRoot.OnElementAdded += (e) => e.eventManager.registerTreeEvent<E>(eventName, eventCallback, registrator, e);
			child.eventManager.registerTreeEvent<E>(eventName, eventCallback, registrator, child);
		}
	}
}
