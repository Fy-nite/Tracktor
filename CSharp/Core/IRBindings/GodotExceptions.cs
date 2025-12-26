using OCRuntime;
using OCRuntime.Runtime;
using System;

namespace Traktor.Core.IRBindings
{
    public class GodotExceptions
    {
        private class ExceptionSignal : Exception
        {
            public object? ExceptionObject { get; }

            public ExceptionSignal(object? exceptionObject)
            {
                ExceptionObject = exceptionObject;
            }
        }

        public IRRuntime Register(IRRuntime runtime)
        {
            runtime.RegisterNativeMethod("GodotExceptions.InvalidCameraException(string)", (instance, args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "Invalid camera exception occurred.";
                var ex = new ManagedObject("GodotExceptions.InvalidCameraException");
                ex.SetField("Message", message);
                return ex;
            });
            runtime.RegisterNativeMethod("GodotExceptions.WorldSyncException(string)", (instance, args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "World sync exception occurred.";
                var ex = new ManagedObject("GodotExceptions.WorldSyncException");
                ex.SetField("Message", message);
                return ex;
            });
            runtime.RegisterNativeMethod("GodotExceptions.InvalidSceneException(string)", (instance, args) =>
            {
                string message = args.Length > 0 && args[0] is string str ? str : "Invalid scene exception occurred.";
                var ex = new ManagedObject("GodotExceptions.InvalidSceneException");
                ex.SetField("Message", message);
                return ex;
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
