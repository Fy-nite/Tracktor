using System;
using System.Net;
using System.Text;
using System.Text.Json;
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
                // status endpoint returning JSON snapshot for UI
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/status")
                {
                    try
                    {
                        var status = V12.Headless.Data.MetricsClient.GetStatus();
                        var json = JsonSerializer.Serialize(status);
                        var data = Encoding.UTF8.GetBytes(json);
                        res.ContentType = "application/json";
                        res.ContentLength64 = data.Length;
                        res.OutputStream.Write(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        var errJson = JsonSerializer.Serialize(new { error = ex.Message });
                        var err = Encoding.UTF8.GetBytes(errJson);
                        res.ContentType = "application/json";
                        res.ContentLength64 = err.Length;
                        res.OutputStream.Write(err, 0, err.Length);
                    }
                }
                // basic web UI that polls /status
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/ui")
                {
                    var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Headless Status</title><style>body{font-family:Arial,Helvetica,sans-serif;margin:20px}table{border-collapse:collapse}td,th{border:1px solid #ddd;padding:8px}</style></head><body>" +
                               "<h1>Headless Status</h1><div id=\"last\">Loading...</div><table id=\"tbl\"><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody></tbody></table><script>async function refresh(){try{let r=await fetch('/status');let j=await r.json();let tb=document.querySelector('#tbl tbody');tb.innerHTML='';for(const k of Object.keys(j)){let v=j[k];let tr=document.createElement('tr');let td1=document.createElement('td');td1.textContent=k;let td2=document.createElement('td');td2.textContent=typeof v==='object'?JSON.stringify(v):v;tr.appendChild(td1);tr.appendChild(td2);tb.appendChild(tr);}document.getElementById('last').textContent='Last updated: '+new Date().toLocaleTimeString();}catch(e){document.getElementById('last').textContent='Error: '+e;} }setInterval(refresh,2000);refresh();</script></body></html>";
                    var data = Encoding.UTF8.GetBytes(html);
                    res.ContentType = "text/html; charset=utf-8";
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
