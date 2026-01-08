using Godot;
using System;

namespace V12.Bindings
{
    /// <summary>
    /// Godot-backed engine bridge using Godot types directly.
    /// </summary>
    public class GodotEngineBridge : IEngineBridge
    {
        private readonly Node? _root;
        private readonly MainThreadDispatcher? _dispatcher;

        public GodotEngineBridge(Node? root = null)
        {
            _root = root;
            _dispatcher = new MainThreadDispatcher();
            if (_root != null)
            {
                _root.AddChild(_dispatcher);
            }
            else
            {
                var main = Engine.GetMainLoop();
                if (main != null)
                {
                    var tree = main as SceneTree;
                    tree?.Root?.AddChild(_dispatcher);
                }
            }
        }

        public object? Instantiate(string scenePath)
        {
            var packed = ResourceLoader.Load<PackedScene>(scenePath);
            if (packed == null) return null;
            var inst = packed.Instantiate();
            if (inst is Node n)
            {
                if (_root != null) _root.AddChild(n);
                else
                {
                    var tree = Engine.GetMainLoop() as SceneTree;
                    tree?.Root?.AddChild(n);
                }
            }
            return inst;
        }

        public object? InstantiateToParent(string scenePath, object? parent)
        {
            var inst = Instantiate(scenePath);
            if (inst is Node n && parent is Node p)
            {
                p.AddChild(n);
            }
            return inst;
        }

        public void Destroy(object? instance)
        {
            if (instance == null) return;
            if (instance is Node n)
            {
                n.QueueFree();
                return;
            }
            // best-effort: nothing else we can safely do without typed engine references
        }

        public object? Find(string pathOrName)
        {
            if (_root == null) return null;
            try
            {
                return _root.GetNodeOrNull(pathOrName);
            }
            catch
            {
                return null;
            }
        }

        public object? GetResource(string path)
        {
            return ResourceLoader.Load(path);
        }

        public void AddToParent(object? parent, object? child)
        {
            if (parent is Node p && child is Node c) p.AddChild(c);
        }

        public void AddToRoot(object? child)
        {
            if (child is Node n)
            {
                if (_root != null) _root.AddChild(n);
                else
                {
                    var tree = Engine.GetMainLoop() as SceneTree;
                    tree?.Root?.AddChild(n);
                }
            }
        }

        public object? GetMainRoot()
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            return tree?.Root;
        }

        public void EmitSignal(object? target, string signal, params object?[] args)
        {
            if (target is Node n)
            {
                try
                {
                    var mi = typeof(Node).GetMethod("EmitSignal");
                    if (mi != null)
                    {
                        var invokeArgs = new object?[1 + (args?.Length ?? 0)];
                        invokeArgs[0] = signal;
                        if (args != null)
                            for (int i = 0; i < args.Length; i++) invokeArgs[1 + i] = args[i];
                        mi.Invoke(n, invokeArgs);
                    }
                }
                catch (Exception ex) { GD.PrintErr($"[GodotEngineBridge] EmitSignal error: {ex.Message}"); }
            }
        }

        public void CallDeferred(object? target, string methodName, params object?[] args)
        {
            if (target is Node n)
            {
                try
                {
                    var mi = typeof(Node).GetMethod("CallDeferred");
                    if (mi != null)
                    {
                        var invokeArgs = new object?[1 + (args?.Length ?? 0)];
                        invokeArgs[0] = methodName;
                        if (args != null)
                            for (int i = 0; i < args.Length; i++) invokeArgs[1 + i] = args[i];
                        mi.Invoke(n, invokeArgs);
                    }
                }
                catch (Exception ex) { GD.PrintErr($"[GodotEngineBridge] CallDeferred error: {ex.Message}"); }
            }
        }

        public void SetVisible(object? node, bool visible)
        {
            if (node is CanvasItem ci) ci.Visible = visible;
        }

        public void SetProcessing(object? node, bool processing)
        {
            if (node is Node n) n.SetProcess(processing);
        }

        public void MoveChild(object? parent, object? child, int toIndex)
        {
            if (parent is Node p && child is Node c)
            {
                var idx = p.GetChildren().IndexOf(c);
                if (idx != -1)
                {
                    p.MoveChild(c, toIndex);
                }
            }
        }

        public object? CreateViewport(string name)
        {
            var vp = new SubViewport();
            vp.Name = name;
            return vp;
        }

        public void SetParent(object? child, object? parent)
        {
            if (child is Node c && parent is Node p)
            {
                p.AddChild(c);
            }
        }

        public void SetName(object? node, string name)
        {
            if (node is Node n) n.Name = name;
        }

        public int GetChildCount(object? parent)
        {
            if (parent is Node p) return p.GetChildCount();
            return 0;
        }

        public object? CreateNode3D(string name = null)
        {
            var node = new Node3D();
            if (!string.IsNullOrEmpty(name)) node.Name = name;
            return node;
        }

        public void SetNode3DTransform(object? node, Godot.Vector3 position, Godot.Vector3 rotation, Godot.Vector3 scale)
        {
            if (node is Node3D n)
            {
                n.Position = position;
                n.Rotation = rotation;
                n.Scale = scale;
            }
        }

        public void CallOnMainThread(Action action)
        {
            if (_dispatcher != null)
            {
                _dispatcher.EnqueueAction(action);
                return;
            }
            try { action(); } catch (Exception ex) { GD.PrintErr($"[GodotEngineBridge] Action error: {ex.Message}"); }
        }

        public void SendMessage(string topic, object? payload = null)
        {
            GD.Print($"[GodotEngineBridge] SendMessage: {topic} payload={payload}");
        }
    }
}
