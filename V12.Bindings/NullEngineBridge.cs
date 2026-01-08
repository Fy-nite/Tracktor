using System;

namespace V12.Bindings
{
    /// <summary>
    /// Default no-op bridge used when no real engine bridge is provided.
    /// Useful for headless tests.
    /// </summary>
    public class NullEngineBridge : IEngineBridge
    {
        public object? Instantiate(string scenePath)
        {
            Console.WriteLine($"[NullEngineBridge] Instantiate called: {scenePath}");
            return null;
        }

        public object? InstantiateToParent(string scenePath, object? parent)
        {
            Console.WriteLine($"[NullEngineBridge] InstantiateToParent: {scenePath}");
            return null;
        }

        public void Destroy(object? instance)
        {
            Console.WriteLine("[NullEngineBridge] Destroy called");
        }

        public object? Find(string pathOrName)
        {
            Console.WriteLine($"[NullEngineBridge] Find called: {pathOrName}");
            return null;
        }

        public object? GetResource(string path)
        {
            Console.WriteLine($"[NullEngineBridge] GetResource: {path}");
            return null;
        }

        public void AddToParent(object? parent, object? child)
        {
            Console.WriteLine("[NullEngineBridge] AddToParent called");
        }

        public void AddToRoot(object? child)
        {
            Console.WriteLine("[NullEngineBridge] AddToRoot called");
        }

        public object? GetMainRoot()
        {
            Console.WriteLine("[NullEngineBridge] GetMainRoot called");
            return null;
        }

        public void EmitSignal(object? target, string signal, params object?[] args)
        {
            Console.WriteLine($"[NullEngineBridge] EmitSignal: {signal}");
        }

        public void CallDeferred(object? target, string methodName, params object?[] args)
        {
            Console.WriteLine($"[NullEngineBridge] CallDeferred: {methodName}");
        }

        public void SetVisible(object? node, bool visible)
        {
            Console.WriteLine($"[NullEngineBridge] SetVisible: {visible}");
        }

        public void SetProcessing(object? node, bool processing)
        {
            Console.WriteLine($"[NullEngineBridge] SetProcessing: {processing}");
        }

        public void MoveChild(object? parent, object? child, int toIndex)
        {
            Console.WriteLine($"[NullEngineBridge] MoveChild to {toIndex}");
        }

        public object? CreateViewport(string name)
        {
            Console.WriteLine($"[NullEngineBridge] CreateViewport: {name}");
            return null;
        }

        public void SetParent(object? child, object? parent)
        {
            Console.WriteLine("[NullEngineBridge] SetParent called");
        }

        public void SetName(object? node, string name)
        {
            Console.WriteLine($"[NullEngineBridge] SetName: {name}");
        }

        public int GetChildCount(object? parent)
        {
            Console.WriteLine("[NullEngineBridge] GetChildCount called");
            return 0;
        }

        public object? CreateNode3D(string name = null)
        {
            Console.WriteLine($"[NullEngineBridge] CreateNode3D: {name}");
            return null;
        }

        public void SetNode3DTransform(object? node, Godot.Vector3 position, Godot.Vector3 rotation, Godot.Vector3 scale)
        {
            Console.WriteLine("[NullEngineBridge] SetNode3DTransform called");
        }

        public void CallOnMainThread(Action action)
        {
            // In tests/headless we can call directly
            try { action(); } catch (Exception ex) { Console.WriteLine($"[NullEngineBridge] Action error: {ex.Message}"); }
        }

        public void SendMessage(string topic, object? payload = null)
        {
            Console.WriteLine($"[NullEngineBridge] SendMessage: {topic} payload={payload}");
        }
    }
}
