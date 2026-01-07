using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using V12.Headless.Messages;

namespace V12.Headless
{
    public sealed class ChatClient : IDisposable
    {
        private readonly Dictionary<string, Registration> _registrations = new(StringComparer.OrdinalIgnoreCase);
        private readonly UdpClient _udp;
        private readonly IPEndPoint _server;
        private readonly CancellationTokenSource _cts = new();
        private Task? _listener;

        public event Action<ChatMessage>? OnChatReceived;
        public event Action<ElementDto>? OnElementReceived;
        public event Action<V12.Headless.Messages.WorldDto>? OnWorldReceived;
        public event Action<V12.Headless.WorldProto>? OnFullWorldReceived;
        public event Action<V12.Headless.FullElementDto>? OnElementProtoReceived;
        public event Action<string, ReadOnlyMemory<byte>>? OnUnhandledMessage;

        private class Registration
        {
            public Func<ReadOnlyMemory<byte>, IMessage> Parse { get; }
            public Func<IMessage, Task> Handle { get; }

            public Registration(Func<ReadOnlyMemory<byte>, IMessage> parse, Func<IMessage, Task> handle)
            {
                Parse = parse;
                Handle = handle;
            }
        }

        public ChatClient(string host, int port, int localPort = 0)
        {
            _udp = new UdpClient(localPort);
            _server = new IPEndPoint(IPAddress.Parse(host), port);

            // Default message handlers
            Register(ChatMessage.Parser, msg =>
            {
                OnChatReceived?.Invoke(msg);
                return Task.CompletedTask;
            });

            Register(ElementDto.Parser, msg =>
            {
                OnElementReceived?.Invoke(msg);
                return Task.CompletedTask;
            });

            // world messages
            Register(V12.Headless.Messages.WorldDto.Parser, msg =>
            {
                OnWorldReceived?.Invoke(msg);
                return Task.CompletedTask;
            });

            // full world proto (generated from .proto)
            Register(V12.Headless.WorldProto.Parser, msg =>
            {
                OnFullWorldReceived?.Invoke(msg);
                return Task.CompletedTask;
            });

            // incremental element proto updates
            Register(V12.Headless.FullElementDto.Parser, msg =>
            {
                OnElementProtoReceived?.Invoke(msg);
                return Task.CompletedTask;
            });
        }

        public Task RequestWorldAsync()
        {
            var req = new V12.Headless.Messages.RequestWorld();
            return SendAsync(req);
        }

        public void Register<T>(MessageParser<T> parser, Func<T, Task> handler, string? name = null) where T : class, IMessage<T>
        {
            if (parser == null) throw new ArgumentNullException(nameof(parser));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var key = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name;
            _registrations[key] = new Registration(
                payload => parser.ParseFrom(payload.ToArray()),
                message => handler((T)message));
        }

        public Task ConnectAsync()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Client already connected");
            }

            _listener = Task.Run(ListenAsync, _cts.Token);
            return Task.CompletedTask;
        }

        public Task SendChatMessageAsync(string user, string text)
        {
            var msg = new ChatMessage
            {
                User = user,
                Text = text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            return SendAsync(msg);
        }

        public Task SendElementAsync(ElementDto element) => SendAsync(element);

        public async Task SendAsync<T>(T message, string? name = null) where T : IMessage
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
                    Console.WriteLine($"[ChatClient] Receive failed: {ex.Message}");
                    continue;
                }

                if (!TryDecodeEnvelope(result.Buffer, out var messageName, out var payload))
                {
                    Console.WriteLine("[ChatClient] Dropping invalid packet (no envelope)");
                    continue;
                }

                if (!_registrations.TryGetValue(messageName, out var registration))
                {
                    OnUnhandledMessage?.Invoke(messageName, payload);
                    continue;
                }

                try
                {
                    var message = registration.Parse(payload);
                    await registration.Handle(message).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatClient] Error handling {messageName}: {ex.Message}");
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
