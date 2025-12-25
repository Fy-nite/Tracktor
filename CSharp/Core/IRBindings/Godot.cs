using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            Runtime.EnableReflectionNativeMethods = true;
            Runtime.RegisterNativeMethod("GD.Print(string)", (arg) =>
            {
                GD.Print(arg[0]);
                return null;
            });
            Runtime.RegisterNativeMethod("GetNode(string)", (arg) =>
            {
                var nodePath = arg[0].ToString();
                if (nodePath == null) return null;
                var node = GetTree().Root.GetNode(nodePath);
                if (node != null)
                {
                    return node;

                }
                return null;
            });
            Runtime.RegisterNativeMethod("GetField(object, string)", (arg) =>
            {
                var obj = arg[0];
                var fieldName = arg[1]?.ToString();
                if (obj == null || fieldName == null) return null;
                var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                return field?.GetValue(obj);
            });
            Runtime.RegisterNativeMethod("SetField(object, string, object)", (arg) =>
            {
                var obj = arg[0];
                var fieldName = arg[1]?.ToString();
                var value = arg[2];
                if (obj == null || fieldName == null) return null;
                var field = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(obj, value);
                return null;
            });
            Runtime.RegisterNativeMethod("Godot.Node.get_Name()", (arg) =>
            {
                var node = arg[0] as Node;
                return (string)node?.Name;
            });
        }
    }
}
