using System;

namespace V12.Bindings
{
    public static class EngineBridgeProvider
    {
        /// <summary>
        /// Holds the active engine bridge. Set this at engine startup.
        /// </summary>
        public static IEngineBridge? Instance { get; set; }
    }
}
