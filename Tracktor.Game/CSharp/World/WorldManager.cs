using V12;
using V12.Interfaces;
using V12.Types;

namespace Tracktor.Tracktor.Game.CSharp.World
{
    /// <summary>
    /// Simple global manager to keep track of the currently active world and
    /// provide quick access to its Syncer and root element.
    /// </summary>
    public static class WorldManager
    {
        public static V12World? CurrentWorld { get; private set; }

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
