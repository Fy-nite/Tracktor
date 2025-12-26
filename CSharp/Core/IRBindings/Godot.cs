using System.Reflection;
using Godot;
using OCRuntime;
namespace Traktor.Core.IRBindings
{
    public partial class GodotBindings : Node
    {
        /// <summary>
        /// Registers methods using the provided IRRuntime instance.
        /// </summary>
        /// <remarks>This function registers common methods available to users to allow people to run ObjectIR within Godot</remarks>
        /// 
        /// <param name="Runtime">The runtime environment in which to register native methods. Cannot be null.</param>
        public void Register(IRRuntime Runtime)
        {
            new GodotExceptions().Register(Runtime);

            Runtime.EnableReflectionNativeMethods = true;
            Runtime.RegisterNativeMethod("GD.Print(string)", (instance, args) =>
            {
                GD.Print(args.Length > 0 ? args[0] : null);
                return null;
            });
            Runtime.RegisterNativeMethod("GetNode(string)", (instance, args) =>
            {
                var nodePath = args.Length > 0 ? args[0]?.ToString() : null;
                if (nodePath == null) return null;
                var node = GetTree().Root.GetNode(nodePath);
                if (node != null)
                {
                    return node;
                }
                return null;
            });
            Runtime.RegisterNativeMethod("GetField(object, string)", (instance, args) =>
            {
                var obj = args[0];
                var fieldName = args[1]?.ToString();
                if (obj == null || fieldName == null) return null;
                var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return field?.GetValue(obj);
            });
            Runtime.RegisterNativeMethod("SetField(object, string, object)", (instance, args) =>
            {
                var obj = args[0];
                var fieldName = args[1]?.ToString();
                var value = args[2];
                if (obj == null || fieldName == null) return null;
                var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(obj, value);
                return null;
            });
            Runtime.RegisterNativeMethod("Godot.Node.get_Name()", (instance, args) =>
            {
                var node = args[0] as Node;
                return node?.Name.ToString() ?? "";
            });

            Runtime.RegisterNativeMethod("GD.PushError(string)", (instance, args) =>
            {
                GD.PushError(args.Length > 0 ? args[0] : null);
                return null;
            });
            Runtime.RegisterNativeMethod("GD.PushWarning(string)", (instance, args) =>
            {
                GD.PushWarning(args.Length > 0 ? args[0] : null);
                return null;
            });
            Runtime.RegisterNativeMethod("Godot.Node.QueueFree()", (instance, args) =>
            {
                var node = args[0] as Node;
                node?.QueueFree();
                return null;
            });
            Runtime.RegisterNativeMethod("Godot.Node.GetParent()", (instance, args) =>
            {
                var node = args[0] as Node;
                return node?.GetParent();
            });
            Runtime.RegisterNativeMethod("Godot.Node.AddChild(object)", (instance, args) =>
            {
                var node = args[0] as Node;
                var child = args.Length > 1 ? args[1] as Node : null;
                if (node != null && child != null)
                {
                    node.AddChild(child);
                }
                return null;
            });
            Runtime.RegisterNativeMethod("Godot.Node.GetChildCount()", (instance, args) =>
            {
                var node = args[0] as Node;
                return node?.GetChildCount() ?? 0;
            });
            Runtime.RegisterNativeMethod("Godot.Node.GetChild(int32)", (instance, args) =>
            {
                var node = args[0] as Node;
                var idx = args.Length > 1 && args[1] is int i ? i : 0;
                return node?.GetChild(idx);
            });
            Runtime.RegisterNativeMethod("Godot.Object.GetInstanceId()", (instance, args) =>
            {
                var obj = args[0] as Godot.GodotObject;
                return obj?.GetInstanceId() ?? 0;
            });

            // Property accessors
            Runtime.RegisterNativeMethod("GetProperty(object, string)", (instance, args) =>
            {
                var obj = args[0];
                var propName = args[1]?.ToString();
                if (obj == null || propName == null) return null;
                var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return prop?.GetValue(obj);
            });

            Runtime.RegisterNativeMethod("SetProperty(object, string, object)", (instance, args) =>
            {
                var obj = args[0];
                var propName = args[1]?.ToString();
                var value = args[2];
                if (obj == null || propName == null) return null;
                var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null)
                {
                    if (value != null && !prop.PropertyType.IsAssignableFrom(value.GetType()) && value is IConvertible)
                    {
                        try
                        {
                            value = Convert.ChangeType(value, prop.PropertyType);
                        }
                        catch { }
                    }
                    prop.SetValue(obj, value);
                }
                return null;
            });

            // Basic Types
            Runtime.RegisterNativeMethod("Godot.Vector3.new(float, float, float)", (instance, args) =>
            {
                float x = args.Length > 0 ? Convert.ToSingle(args[0]) : 0;
                float y = args.Length > 1 ? Convert.ToSingle(args[1]) : 0;
                float z = args.Length > 2 ? Convert.ToSingle(args[2]) : 0;
                return new Vector3(x, y, z);
            });

            Runtime.RegisterNativeMethod("Godot.CSGBox3D.new()", (instance, args) => new Godot.CsgBox3D());
            
        }
    }
}
