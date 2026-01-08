using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeadlessMiddleWare
{
    public static class SimpleHttpServer
    {
        public static Task StartAsync(int port, int udpPort, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                var prefix = $"http://*:{port}/";
                using var listener = new HttpListener();
                listener.Prefixes.Add(prefix);
                listener.Start();

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                        _ = Task.Run(() => HandleContext(ctx, udpPort, token));
                    }
                    catch (HttpListenerException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SimpleHttpServer] Listener error: {ex.Message}");
                        await Task.Delay(1000, token).ConfigureAwait(false);
                    }
                }

                try { listener.Stop(); } catch { }
            }, token);
        }

        private static void HandleContext(HttpListenerContext ctx, int udpPort, CancellationToken token)
        {
            try
            {
                var req = ctx.Request;
                var res = ctx.Response;

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/")
                {
                    var body = $"{{\"status\":\"ok\",\"udpPort\":{udpPort}}}";
                    var data = Encoding.UTF8.GetBytes(body);
                    res.ContentType = "application/json";
                    res.ContentLength64 = data.Length;
                    res.OutputStream.Write(data, 0, data.Length);
                }
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/health")
                {
                    var data = Encoding.UTF8.GetBytes("healthy");
                    res.ContentType = "text/plain";
                    res.ContentLength64 = data.Length;
                    res.OutputStream.Write(data, 0, data.Length);
                }
                else if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/shutdown")
                {
                    var data = Encoding.UTF8.GetBytes("{\"shuttingDown\":true}");
                    res.ContentType = "application/json";
                    res.ContentLength64 = data.Length;
                    res.OutputStream.Write(data, 0, data.Length);
                }
                // prometheus metrics endpoint
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/metrics")
                {
                    var body = "# HELP headless_udp_port UDP port the headless server is listening on\n" +
                               "# TYPE headless_udp_port gauge\n" +
                               $"headless_udp_port {udpPort}\n";
                    var data = Encoding.UTF8.GetBytes(body);
                    res.ContentType = "text/plain; version=0.0.4";
                    res.ContentLength64 = data.Length;
                    res.OutputStream.Write(data, 0, data.Length);
                }
                else
                {
                    res.StatusCode = 404;
                }

                res.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimpleHttpServer] Error handling request: {ex.Message}");
            }
        }
    }
}
