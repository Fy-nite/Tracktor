using OCRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traktor.Core.IRBindings
{
    public class GodotExceptions
    {
        public GodotExceptions() { }
        public IRRuntime Register(IRRuntime runtime)
        {
            runtime.RegisterNativeMethod("GodotExceptions.InvalidCameraException(string)", (args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "Invalid camera exception occurred.";
                throw new InvalidCameraException(message);
            });
            runtime.RegisterNativeMethod("GodotExceptions.WorldSyncException(string)", (args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "World sync exception occurred.";
                throw new WorldSyncException(message);
            });
            runtime.RegisterNativeMethod("GodotExceptions.InvalidSceneException(string)", (args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "Invalid scene exception occurred.";
                throw new InvalidSceneException(message);
            });
            return runtime;
        }
        public class InvalidSceneException : Exception
        {
            public InvalidSceneException(string message) : base(message) { }
        }

        public class InvalidCameraException : Exception
        {
            public InvalidCameraException(string message) : base(message) { }
        } 

        public class WorldSyncException : Exception
        {
            public WorldSyncException(string message) : base(message) { }
        }
    }
}
