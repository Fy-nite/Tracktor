//using System;
//using System.Reflection;

//namespace V12.Bindings
//{
//    /// <summary>
//    /// Lightweight Unity bridge using reflection so V12.Bindings does not need a compile-time dependency on Unity.
//    /// </summary>
//    public class UnityEngineBridge : IEngineBridge
//    {
//        private readonly object? _dispatcher;
//        private readonly Assembly? _unityAssembly;

//        public UnityEngineBridge(object? dispatcher = null)
//        {
//            _dispatcher = dispatcher;
//            _unityAssembly = TryLoadUnityAssembly();
//        }

//        public object? InstantiateToParent(string scenePath, object? parent)
//        {
//            var inst = Instantiate(scenePath);
//            // cannot easily parent via reflection in a reliable cross-version way here; noop
//            return inst;
//        }

//        private Assembly? TryLoadUnityAssembly()
//        {
//            try
//            {
//                var asm = Assembly.Load("UnityEngine");
//                return asm;
//            }

//        public object? GetResource(string path)
//        {
//            if (_unityAssembly == null) return null;
//            try
//            {
//                var resourcesType = _unityAssembly.GetType("UnityEngine.Resources");
//                var loadMethod = resourcesType?.GetMethod("Load", new[] { typeof(string) });
//                return loadMethod?.Invoke(null, new object[] { path });
//            }
//            catch { return null; }
//        }

//        public void AddToParent(object? parent, object? child) { }
//        public void AddToRoot(object? child) { }
//        public object? GetMainRoot() { return null; }
//        public void EmitSignal(object? target, string signal, params object?[] args) { }
//        public void CallDeferred(object? target, string methodName, params object?[] args) { }
//        public void SetVisible(object? node, bool visible) { }
//        public void SetProcessing(object? node, bool processing) { }
//        public void MoveChild(object? parent, object? child, int toIndex) { }
//            catch
//            {
//                Console.WriteLine("[UnityEngineBridge] UnityEngine assembly not found at runtime.");
//                return null;
//            }
//        }

//        public object? Instantiate(string scenePath)
//        {
//            if (_unityAssembly == null)
//            {
//                Console.WriteLine($"[UnityEngineBridge] Instantiate: Unity not available for {scenePath}");
//                return null;
//            }

//            try
//            {
//                var resourcesType = _unityAssembly.GetType("UnityEngine.Resources");
//                if (resourcesType == null) return null;
//                var loadMethod = resourcesType.GetMethod("Load", new[] { typeof(string) });
//                if (loadMethod == null) return null;
//                var prefab = loadMethod.Invoke(null, new object[] { scenePath });
//                if (prefab == null) return null;
//                var gameObjectType = _unityAssembly.GetType("UnityEngine.GameObject") ?? prefab.GetType();
//                var instantiateMethod = _unityAssembly.GetType("UnityEngine.Object")?.GetMethod("Instantiate", new[] { typeof(object) });
//                if (instantiateMethod == null) return null;
//                var instance = instantiateMethod.Invoke(null, new object[] { prefab });
//                return instance;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[UnityEngineBridge] Instantiate error: {ex.Message}");
//                return null;
//            }
//        }

//        public void Destroy(object? instance)
//        {
//            if (_unityAssembly == null)
//            {
//                Console.WriteLine("[UnityEngineBridge] Destroy: Unity not available");
//                return;
//            }

//            try
//            {
//                if (instance == null) return;
//                var objectType = _unityAssembly.GetType("UnityEngine.Object");
//                var destroyMethod = objectType?.GetMethod("Destroy", new[] { objectType });
//                if (destroyMethod != null)
//                {
//                    destroyMethod.Invoke(null, new object[] { instance });
//                    return;
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[UnityEngineBridge] Destroy error: {ex.Message}");
//            }
//        }

//        public object? Find(string pathOrName)
//        {
//            if (_unityAssembly == null)
//            {
//                Console.WriteLine($"[UnityEngineBridge] Find: Unity not available for {pathOrName}");
//                return null;
//            }

//            try
//            {
//                var goType = _unityAssembly.GetType("UnityEngine.GameObject");
//                var findMethod = goType?.GetMethod("Find", new[] { typeof(string) });
//                if (findMethod != null)
//                {
//                    return findMethod.Invoke(null, new object[] { pathOrName });
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[UnityEngineBridge] Find error: {ex.Message}");
//            }

//            return null;
//        }

//        public void CallOnMainThread(Action action)
//        {
//            if (_unityAssembly == null)
//            {
//                try { action(); } catch (Exception ex) { Console.WriteLine($"[UnityEngineBridge] Action error: {ex.Message}"); }
//                return;
//            }

//            try
//            {
//                // If a dispatcher GameObject/MonoBehaviour was provided, try to call a method that schedules on main thread
//                if (_dispatcher != null)
//                {
//                    var t = _dispatcher.GetType();
//                    var schedule = t.GetMethod("ScheduleOnMainThread");
//                    if (schedule != null)
//                    {
//                        schedule.Invoke(_dispatcher, new object[] { action });
//                        return;
//                    }
//                }

//                // fallback: try Unity's SynchronizationContext
//                var syncType = typeof(System.Threading.SynchronizationContext);
//                var ctx = System.Threading.SynchronizationContext.Current;
//                if (ctx != null)
//                {
//                    ctx.Post(_ => action(), null);
//                    return;
//                }

//                // last resort: call directly
//                action();
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[UnityEngineBridge] CallOnMainThread error: {ex.Message}");
//            }
//        }

//        public void SendMessage(string topic, object? payload = null)
//        {
//            Console.WriteLine($"[UnityEngineBridge] SendMessage: {topic} payload={payload}");
//        }
//    }
//}
