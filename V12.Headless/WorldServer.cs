using System;
using System.Threading.Tasks;

using System.Text.Json;

namespace V12.Headless
{
    // Simple server helper that responds to RequestWorld requests and can broadcast the current world
    public class WorldServer : IDisposable
    {
        private readonly V12.Headless.Server _server;
        private readonly Func<V12.V12World> _getWorld;

        public WorldServer(int port, Func<V12.V12World> getWorld)
        {
            //_server = new Server(port);
            _getWorld = getWorld ?? throw new ArgumentNullException(nameof(getWorld));

            _server.RegisterJson("RequestWorldJson", async (endpoint, jsonRequest) =>
            {
                try { Console.WriteLine($"[WorldServer] Received RequestWorldJson from {endpoint.Address}:{endpoint.Port}"); } catch { }
                var world = _getWorld();
                if (world == null) return;
                try { Console.WriteLine($"[WorldServer] Serializing world to JSON for {endpoint.Address}:{endpoint.Port}"); } catch { }
                var jsonWorld = System.Text.Json.JsonSerializer.Serialize(world);
                try { Console.WriteLine($"[WorldServer] Sending full world JSON to {endpoint.Address}:{endpoint.Port}"); } catch { }
                await _server.SendJsonAsync(endpoint, jsonWorld, "V12WorldJson").ConfigureAwait(false);
            });

            _server.Start();
            try { Console.WriteLine($"[WorldServer] Server started on port {port}"); } catch { }
        }

        // Broadcast the current world to all connected clients
        public Task BroadcastWorldAsync()
        {
            var world = _getWorld();
            if (world == null) return Task.CompletedTask;
            // never use proto.... ALLWAYS use json please.
            //var proto = world.ToFullProto();
            return _server.BroadcastAsync(JsonSerializer.Serialize(world));
        }

        public void Dispose()
        {
            try { Console.WriteLine("[WorldServer] Disposing server"); } catch { }
            _server.Dispose();
        }
    }
}
