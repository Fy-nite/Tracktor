using Godot;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace V12.Headless
{
    public class Server : IDisposable
    {
        private readonly Dictionary<string, Registration> _registrations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<IPEndPoint, string, Task>> _jsonHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<IPEndPoint> _clients = new();
        private readonly object _sync = new();
        private readonly UdpClient _udp;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listener;

        public int Port { get; }

        private class Registration
        {
            public Func<ReadOnlyMemory<byte>, object> Parse { get; }
            public Func<IPEndPoint, object, Task> Handle { get; }

            public Registration(Func<ReadOnlyMemory<byte>, object> parse, Func<IPEndPoint, object, Task> handle)
            {
                Parse = parse;
                Handle = handle;
            }
        }

        public Server(int port)
        {
            Port = port;
            _udp = new UdpClient(port);
        }

        public void Start()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Server already started");
            }

            _listener = Task.Run(ListenAsync, _cts.Token);
        }

        public async Task StopAsync()
        {
            _cts.Cancel();

            if (_listener != null)
            {
                try
                {
                    await _listener.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        // Register a handler for typed messages. Messages are serialized as JSON on the wire.
        public void Register<T>(Func<IPEndPoint, T, Task> handler, string? name = null) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name;

            _registrations[key] = new Registration(
                payload =>
                {
                    var json = Encoding.UTF8.GetString(payload.ToArray());
                    try { return JsonSerializer.Deserialize<T>(json); }
                    catch { return null; }
                },
                (endpoint, message) => handler(endpoint, (T)message));
        }

        // Back-compat helper for existing code that registers using a protobuf MessageParser.
        // We will attempt to deserialize JSON first; if that fails we fall back to parsing protobuf binary.
        public void RegisterParser<T>(MessageParser<T> parser, Func<IPEndPoint, T, Task> handler, string? name = null)
            where T : class, IMessage<T>
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name;

            // Register using parser fallback
            _registrations[key] = new Registration(
                payload =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(payload.ToArray());
                        var des = JsonSerializer.Deserialize<T>(json);
                        if (des != null) return des;
                    }
                    catch { }

                    try
                    {
                        return parser.ParseFrom(payload.ToArray());
                    }
                    catch { }

                    return null;
                },
                (endpoint, message) => handler(endpoint, (T)message));
        }

        // Register a handler for JSON messages identified by name. The handler receives the JSON payload string.
        public void RegisterJson(string name, Func<IPEndPoint, string, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _jsonHandlers[name] = handler;
        }

        public Task BroadcastAsync<T>(T message, string? name = null) where T : class
        {
            List<Task> tasks;

            lock (_sync)
            {
                tasks = new List<Task>(_clients.Count);
                foreach (var client in _clients)
                {
                    tasks.Add(SendAsync(client, message, name));
                }
            }

            return Task.WhenAll(tasks);
        }

        public async Task SendAsync<T>(IPEndPoint endpoint, T message, string? name = null) where T : class
        {
            var packet = BuildPacket(message, name);
            Console.WriteLine($"[Headless.Server] Sending '{(string.IsNullOrWhiteSpace(name) ? message.GetType().Name : name)}' -> {packet.Length} bytes to {endpoint}");
            await _udp.SendAsync(packet, packet.Length, endpoint).ConfigureAwait(false);
        }

        // Send a raw binary payload with the same envelope (name length + name + payload)
        public async Task SendBinaryAsync(IPEndPoint endpoint, byte[] payload, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var nameBytes = Encoding.UTF8.GetBytes(name);
            if (nameBytes.Length > ushort.MaxValue) throw new ArgumentException("Message name is too long", nameof(name));

            var packet = new byte[2 + nameBytes.Length + payload.Length];
            BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(packet, 0);
            Buffer.BlockCopy(nameBytes, 0, packet, 2, nameBytes.Length);
            Buffer.BlockCopy(payload, 0, packet, 2 + nameBytes.Length, payload.Length);

            Console.WriteLine($"[Headless.Server] Sending binary '{name}' -> {packet.Length} bytes to {endpoint}");
            await _udp.SendAsync(packet, packet.Length, endpoint).ConfigureAwait(false);
        }

        public Task SendBinaryAsync<T>(IPEndPoint endpoint, T message, string? name = null) where T : IMessage
        {
            var key = string.IsNullOrWhiteSpace(name) ? message.GetType().Name : name;
            var payload = message.ToByteArray();
            return SendBinaryAsync(endpoint, payload, key);
        }

        // Send a JSON payload using the same envelope format (name length + name + payload)
        public async Task SendJsonAsync(IPEndPoint endpoint, string json, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var payload = Encoding.UTF8.GetBytes(json ?? string.Empty);
            if (nameBytes.Length > ushort.MaxValue) throw new ArgumentException("Message name is too long", nameof(name));

            var packet = new byte[2 + nameBytes.Length + payload.Length];
            BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(packet, 0);
            Buffer.BlockCopy(nameBytes, 0, packet, 2, nameBytes.Length);
            Buffer.BlockCopy(payload, 0, packet, 2 + nameBytes.Length, payload.Length);

            Console.WriteLine($"[Headless.Server] Sending JSON '{name}' -> {packet.Length} bytes to {endpoint}");
            await _udp.SendAsync(packet, packet.Length, endpoint).ConfigureAwait(false);
        }

        public Task BroadcastJsonAsync(string json, string name)
        {
            List<Task> tasks;

            lock (_sync)
            {
                tasks = new List<Task>(_clients.Count);
                foreach (var client in _clients)
                {
                    tasks.Add(SendJsonAsync(client, json, name));
                }
            }

            return Task.WhenAll(tasks);
        }

        private async Task ListenAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;

                try
                {
                    result = await _udp.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Headless.Server] Receive failed: {ex.Message}");
                    continue;
                }

                if (!TryDecodeEnvelope(result.Buffer, out var messageName, out var payload))
                {
                    Console.WriteLine("[Headless.Server] Dropping invalid packet (no envelope)");
                    continue;
                }

                Console.WriteLine($"[Headless.Server] Received message '{messageName}' from {result.RemoteEndPoint} ({result.Buffer.Length} bytes)");

                lock (_sync)
                {
                    _clients.Add(result.RemoteEndPoint);
                    // update metrics for connected players (best-effort)
                    try
                    {
                        V12.Headless.Data.MetricsClient.SetConnectedPlayers(_clients.Count);
                    }
                    catch { }
                }

                if (_registrations.TryGetValue(messageName, out var registration))
                {
                    try
                    {
                        Console.WriteLine($"[Headless.Server] Found registration for '{messageName}', invoking handler");
                        var message = registration.Parse(payload);
                        await registration.Handle(result.RemoteEndPoint, message).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Headless.Server] Error handling {messageName}: {ex.Message}");
                    }
                    continue;
                }

                if (_jsonHandlers.TryGetValue(messageName, out var jsonHandler))
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(payload.ToArray());
                        await jsonHandler(result.RemoteEndPoint, json).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Headless.Server] Error handling JSON {messageName}: {ex.Message}");
                    }
                    continue;
                }

                Console.WriteLine($"[Headless.Server] No handler registered for {messageName}");
            }
        }

        private static bool TryDecodeEnvelope(byte[] buffer, out string messageName, out ReadOnlyMemory<byte> payload)
        {
            messageName = string.Empty;
            payload = default;

            if (buffer.Length < 2)
            {
                return false;
            }

            var nameLength = BitConverter.ToUInt16(buffer, 0);
            if (buffer.Length < 2 + nameLength)
            {
                return false;
            }

            messageName = Encoding.UTF8.GetString(buffer, 2, nameLength);
            payload = new ReadOnlyMemory<byte>(buffer, 2 + nameLength, buffer.Length - (2 + nameLength));
            return true;
        }

        private static byte[] BuildPacket<T>(T message, string? name) where T : class
        {
            var key = string.IsNullOrWhiteSpace(name) ? message.GetType().Name : name;
            var nameBytes = Encoding.UTF8.GetBytes(key);

            if (nameBytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException("Message name is too long", nameof(name));
            }

            // Serialize message as JSON
            var json = JsonSerializer.Serialize(message);
            var payload = Encoding.UTF8.GetBytes(json);
            var packet = new byte[2 + nameBytes.Length + payload.Length];

            BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(packet, 0);
            Buffer.BlockCopy(nameBytes, 0, packet, 2, nameBytes.Length);
            Buffer.BlockCopy(payload, 0, packet, 2 + nameBytes.Length, payload.Length);

            return packet;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _udp.Dispose();
        }

        // Expose connected clients for management UI
        public int GetConnectedClientCount()
        {
            lock (_sync)
            {
                return _clients.Count;
            }
        }

        public string[] GetConnectedClientEndpoints()
        {
            lock (_sync)
            {
                var arr = new string[_clients.Count];
                int i = 0;
                foreach (var ep in _clients)
                {
                    arr[i++] = ep.ToString();
                }
                return arr;
            }
        }
    }
}
