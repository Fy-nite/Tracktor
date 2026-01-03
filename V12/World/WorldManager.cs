using V12.Interfaces;
using V12.Types;

namespace V12
{
    /// <summary>
    /// Simple global manager to keep track of the currently active world and
    /// provide quick access to its Syncer and root element.
    /// </summary>
    public static class WorldManager
    {
        public static V12World? CurrentWorld { get; private set; }

        public static WorldElementSceneSyncer? CurrentSyncer => CurrentWorld?.Syncer;

        public static Element? RootElement => CurrentWorld?.RootElement;

        public static void SetCurrent(V12World world)
        {
            CurrentWorld = world;
        }

        public static void ClearCurrent()
        {
            CurrentWorld = null;
        }
    }
}
