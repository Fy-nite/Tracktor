using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using V12;
using V12.Types;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using V12;
using V12.Types;

namespace V12.Headless
{
    // Minimal UDP client that sends and receives engine-world JSON envelopes.
    public sealed class WorldClient : IDisposable
    {
        private readonly UdpClient _udp;
        private readonly IPEndPoint _server;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listener;

        public event Action<V12World>? OnFullWorldReceived;
        public event Action<Element>? OnElementReceived;
        public event Action<string>? OnJsonWorldReceived;

        public WorldClient(string host, int port, int localPort = 0)
        {
            _udp = new UdpClient(localPort);
            _server = new IPEndPoint(IPAddress.Parse(host), port);
        }

        public Task ConnectAsync()
        {
            if (_listener != null) throw new InvalidOperationException("Client already connected");
            try
            {
                Console.WriteLine($"[WorldClient] Connecting to server {_server.Address}:{_server.Port}");
            }
            catch { }
            _listener = Task.Run(ListenAsync, _cts.Token);
            try
            {
                Console.WriteLine("[WorldClient] Listener task started");
            }
            catch { }
            return Task.CompletedTask;
        }

        public Task RequestWorldAsync()
        {
            try
            {
                Console.WriteLine("[WorldClient] Requesting full world (RequestWorldJson)");
            }
            catch { }
            return SendJsonAsync("{}", "RequestWorldJson");
        }

        public async Task SendJsonAsync(string json, string name)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var payload = Encoding.UTF8.GetBytes(json ?? string.Empty);
            if (nameBytes.Length > ushort.MaxValue) throw new ArgumentException("Message name is too long", nameof(name));

            var packet = new byte[2 + nameBytes.Length + payload.Length];
            BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(packet, 0);
            Buffer.BlockCopy(nameBytes, 0, packet, 2, nameBytes.Length);
            Buffer.BlockCopy(payload, 0, packet, 2 + nameBytes.Length, payload.Length);

            try
            {
                Console.WriteLine($"[WorldClient] Sending message '{name}' ({payload.Length} bytes) to {_server.Address}:{_server.Port}");
            }
            catch { }
            await _udp.SendAsync(packet, packet.Length, _server).ConfigureAwait(false);
        }

        private async Task ListenAsync()
        {
            try { Console.WriteLine("[WorldClient] Listener running"); } catch { }
            while (!_cts.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udp.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    try { Console.WriteLine("[WorldClient] Listener canceled"); } catch { }
                    break;
                }
                catch (ObjectDisposedException)
                {
                    try { Console.WriteLine("[WorldClient] Listener disposed"); } catch { }
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldClient] Receive failed: {ex.Message}");
                    continue;
                }

                if (!TryDecodeEnvelope(result.Buffer, out var messageName, out var payload))
                {
                    Console.WriteLine("[WorldClient] Dropping invalid packet (no envelope)");
                    continue;
                }

                try
                {
                    try { Console.WriteLine($"[WorldClient] Received message '{messageName}' ({payload.Length} bytes)"); } catch { }
                    switch (messageName)
                    {
                        case "V12WorldJson":
                            try
                            {
                                var json = Encoding.UTF8.GetString(payload.ToArray());
                                var engineWorld = JsonSerializer.Deserialize<V12World>(json);
                                if (engineWorld != null) OnFullWorldReceived?.Invoke(engineWorld);
                                else OnJsonWorldReceived?.Invoke(json);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[WorldClient] Failed to parse V12WorldJson: {ex.Message}");
                                OnJsonWorldReceived?.Invoke(Encoding.UTF8.GetString(payload.ToArray()));
                            }
                            break;
                        case "V12ElementJson":
                            try
                            {
                                var json = Encoding.UTF8.GetString(payload.ToArray());
                                var elem = JsonSerializer.Deserialize<Element>(json);
                                if (elem != null) OnElementReceived?.Invoke(elem);
                                else OnJsonWorldReceived?.Invoke(json);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[WorldClient] Failed to parse V12ElementJson: {ex.Message}");
                                OnJsonWorldReceived?.Invoke(Encoding.UTF8.GetString(payload.ToArray()));
                            }
                            break;
                        default:
                            // ignore unknown messages for now
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WorldClient] Error handling {messageName}: {ex.Message}");
                }
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

        public void Dispose()
        {
            _cts.Cancel();
            _udp.Dispose();
        }
    }
}
