using System;

namespace V12.Bindings
{
    /// <summary>
    /// Minimal engine abstraction so V12 logic can call into host engine (Godot/Unity/etc.).
    /// </summary>
    public interface IEngineBridge
    {
        object? Instantiate(string scenePath);
        object? InstantiateToParent(string scenePath, object? parent);
        void Destroy(object? instance);
        object? Find(string pathOrName);
        object? GetResource(string path);
        void AddToParent(object? parent, object? child);
        void AddToRoot(object? child);
        object? GetMainRoot();
        void EmitSignal(object? target, string signal, params object?[] args);
        void CallDeferred(object? target, string methodName, params object?[] args);
        void SetVisible(object? node, bool visible);
        void SetProcessing(object? node, bool processing);
        void MoveChild(object? parent, object? child, int toIndex);
        object? CreateViewport(string name);
        void SetParent(object? child, object? parent);
        void SetName(object? node, string name);
        int GetChildCount(object? parent);
        /// <summary>
        /// Create a Node3D instance in the engine and return it.
        /// </summary>
        object? CreateNode3D(string name = null);

        /// <summary>
        /// Set transform properties on a Node3D instance.
        /// </summary>
        void SetNode3DTransform(object? node, Godot.Vector3 position, Godot.Vector3 rotation, Godot.Vector3 scale);
        void CallOnMainThread(Action action);
        void SendMessage(string topic, object? payload = null);
    }
}
