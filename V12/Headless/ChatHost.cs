using System;
using System.Net;
using System.Threading.Tasks;
using V12.Headless.Messages;
using V12;
using V12.Types.Models;

namespace V12.Headless
{
    public sealed class ChatHost : IDisposable
    {
        private readonly Server _server;

        public event Action<ChatMessage>? OnChatReceived;
        public event Action<ElementDto>? OnElementReceived;

        public ChatHost(int port = 7777)
        {
            _server = new Server(port);
        }

        public void Start()
        {
            _server.Register(ChatMessage.Parser, async (endpoint, msg) =>
            {
                OnChatReceived?.Invoke(msg);
                await _server.BroadcastAsync(msg).ConfigureAwait(false);
            });

            // allow clients to request the current world (send full WorldProto)
            _server.Register(V12.Headless.Messages.RequestWorld.Parser, async (endpoint, msg) =>
            {
                if (WorldManager.CurrentWorld != null)
                {
                    // map engine world to full proto and send (use static mapper to avoid extension lookup issues)
                    var full = V12.Types.Models.WorldProtoMapper.ToFullProto(WorldManager.CurrentWorld);
                    await _server.SendAsync(endpoint, full).ConfigureAwait(false);
                }
                else
                {
                    // send empty WorldProto to indicate no world
                    await _server.SendAsync(endpoint, new WorldProto()).ConfigureAwait(false);
                }
            });

            // optional: register for incremental element updates from server-side code
            // if server later wants to broadcast element deltas it can use the message name 'FullElementDto'

            _server.Start();
        }

        public Task StopAsync() => _server.StopAsync();

        public Task SendChatAsync(string user, string text)
        {
            var msg = new ChatMessage
            {
                User = user,
                Text = text,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            return _server.BroadcastAsync(msg);
        }

        public Task SendElementAsync(ElementDto element) => _server.BroadcastAsync(element);

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
