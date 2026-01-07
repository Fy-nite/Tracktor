using System;
using System.Threading.Tasks;
using V12.Types.Models;
using V12.Headless.Messages;

namespace V12.Headless
{
    // Simple server helper that responds to RequestWorld requests and can broadcast the current world
    public class WorldServer : IDisposable
    {
        private readonly Server _server;
        private readonly Func<V12.V12World> _getWorld;

        public WorldServer(int port, Func<V12.V12World> getWorld)
        {
            _server = new Server(port);
            _getWorld = getWorld ?? throw new ArgumentNullException(nameof(getWorld));

            // register a handler that replies with the full world when asked
            _server.Register(RequestWorld.Parser, async (endpoint, req) =>
            {
                try
                {
                    var world = _getWorld();
                    if (world == null) return;
                    var proto = world.ToFullProto();
                    await _server.SendAsync(endpoint, proto).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldServer] Error handling RequestWorld: {ex.Message}");
                }
            });

            _server.Start();
        }

        // Broadcast the current world to all connected clients
        public Task BroadcastWorldAsync()
        {
            var world = _getWorld();
            if (world == null) return Task.CompletedTask;
            var proto = world.ToFullProto();
            return _server.BroadcastAsync(proto);
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
