using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using V12.Headless.Messages;

namespace V12.Headless
{
    // Minimal UDP client focused on requesting and receiving worlds and incremental element protos.
    public sealed class WorldClient : IDisposable
    {
        private readonly UdpClient _udp;
        private readonly IPEndPoint _server;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listener;

        public event Action<WorldProto>? OnFullWorldReceived;
        public event Action<FullElementDto>? OnElementProtoReceived;

        public WorldClient(string host, int port, int localPort = 0)
        {
            _udp = new UdpClient(localPort);
            _server = new IPEndPoint(IPAddress.Parse(host), port);
        }

        public Task ConnectAsync()
        {
            if (_listener != null) throw new InvalidOperationException("Client already connected");
            _listener = Task.Run(ListenAsync, _cts.Token);
            return Task.CompletedTask;
        }

        public Task RequestWorldAsync()
        {
            var req = new RequestWorld();
            return SendAsync(req);
        }

        public Task SendWorldAsync(WorldProto proto)
        {
            return SendAsync(proto);
        }

        private async Task SendAsync<T>(T message, string? name = null) where T : IMessage
        {
            var packet = BuildPacket(message, name);
            await _udp.SendAsync(packet, packet.Length, _server).ConfigureAwait(false);
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
                    switch (messageName)
                    {
                        case nameof(WorldProto):
                        //case "WorldProto":
                            var world = WorldProto.Parser.ParseFrom(payload.ToArray());
                            OnFullWorldReceived?.Invoke(world);
                            break;
                        case nameof(FullElementDto):
                        //case "FullElementDto":
                            var el = FullElementDto.Parser.ParseFrom(payload.ToArray());
                            OnElementProtoReceived?.Invoke(el);
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

        private static byte[] BuildPacket<T>(T message, string? name) where T : IMessage
        {
            var key = string.IsNullOrWhiteSpace(name) ? message.GetType().Name : name;
            var nameBytes = Encoding.UTF8.GetBytes(key);

            if (nameBytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException("Message name is too long", nameof(name));
            }

            var payload = message.ToByteArray();
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
    }
}
