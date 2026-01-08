using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace V12.MetricsServer
{
    internal static class Program
    {
        private record MetricEntry(string Name, double Value, Dictionary<string, string> Labels, DateTime Updated);

        private static async Task Main(string[] args)
        {
            // Very small metrics server that accepts POST /push-metrics JSON payloads and exposes /metrics in Prometheus format
            var prefix = "http://localhost:9000/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            Console.WriteLine($"Metrics server listening on {prefix}");

            // key -> MetricEntry
            var store = new ConcurrentDictionary<string, MetricEntry>();
            // track last seen time per headless instance (instance label)
            var instanceLastSeen = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => Handle(ctx, store, instanceLastSeen));
                }
            });

            Console.WriteLine("Press Ctrl+C to exit");
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static void Handle(HttpListenerContext ctx, ConcurrentDictionary<string, MetricEntry> store, ConcurrentDictionary<string, DateTime> instanceLastSeen)
        {
            try
            {
                var req = ctx.Request;
                var res = ctx.Response;

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/push-metrics")
                {
                    Console.WriteLine($"[MetricsServer] /push-metrics received");
                    using var ms = new System.IO.MemoryStream();
                    req.InputStream.CopyTo(ms);
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    Console.WriteLine($"[MetricsServer] Payload: {json}");
                    try
                    {
                        var doc = JsonDocument.Parse(json);

                        // new format: { "metrics": [ { "name": "m", "value": 1, "labels": {"k":"v"} }, ... ] }
                        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("metrics", out var metricsElem) && metricsElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in metricsElem.EnumerateArray())
                            {
                                if (item.ValueKind != JsonValueKind.Object) continue;
                                if (!item.TryGetProperty("name", out var nameEl)) continue;
                                var name = nameEl.GetString() ?? string.Empty;
                                if (string.IsNullOrWhiteSpace(name)) continue;

                                double val = 0;
                                if (item.TryGetProperty("value", out var valEl))
                                {
                                    if (valEl.ValueKind == JsonValueKind.Number && valEl.TryGetDouble(out var d)) val = d;
                                    else
                                    {
                                        var s = valEl.ToString();
                                        double.TryParse(s, out val);
                                    }
                                }

                                var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                if (item.TryGetProperty("labels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var p in labelsEl.EnumerateObject())
                                    {
                                        labels[p.Name] = p.Value.ToString().Trim('"');
                                    }
                                }

                                // ensure an instance label
                                if (!labels.ContainsKey("instance")) labels["instance"] = Environment.MachineName;

                                var key = CanonicalKey(name, labels);
                                store[key] = new MetricEntry(name, val, labels, DateTime.UtcNow);
                                if (labels.TryGetValue("instance", out var inst) && !string.IsNullOrWhiteSpace(inst))
                                {
                                    instanceLastSeen[inst] = DateTime.UtcNow;
                                }
                            }
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            // legacy flat format: { "metric": 1, "other": "x" }
                            foreach (var prop in doc.RootElement.EnumerateObject())
                            {
                                var name = prop.Name;
                                var valueEl = prop.Value;
                                if (valueEl.ValueKind == JsonValueKind.Number && valueEl.TryGetDouble(out var d))
                                {
                                    var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["instance"] = Environment.MachineName };
                                    var key = CanonicalKey(name, labels);
                                    store[key] = new MetricEntry(name, d, labels, DateTime.UtcNow);
                                    instanceLastSeen[labels["instance"]] = DateTime.UtcNow;
                                }
                                else if (valueEl.ValueKind == JsonValueKind.String)
                                {
                                    // expose as _info metric with value 1 and label 'value'
                                    var infoName = name + "_info";
                                    var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["value"] = valueEl.GetString() ?? string.Empty, ["instance"] = Environment.MachineName };
                                    var key = CanonicalKey(infoName, labels);
                                    store[key] = new MetricEntry(infoName, 1, labels, DateTime.UtcNow);
                                    instanceLastSeen[labels["instance"]] = DateTime.UtcNow;
                                }
                            }
                        }

                    // compute instances online (seen recently)
                    var ttl = TimeSpan.FromSeconds(90);
                    var now = DateTime.UtcNow;
                    var onlineCount = 0;
                    foreach (var kv in instanceLastSeen)
                    {
                        if (now - kv.Value <= ttl) onlineCount++;
                    }

                    var sb = new StringBuilder();

                    sb.AppendLine("# HELP headless_instances_online Number of headless instances that pushed metrics recently");
                    sb.AppendLine("# TYPE headless_instances_online gauge");
                    sb.AppendLine($"headless_instances_online {onlineCount}");

                    // emit per-instance last seen as unix timestamp
                    sb.AppendLine("# HELP headless_instance_last_seen_seconds Last seen time for headless instance (unix seconds)");
                    sb.AppendLine("# TYPE headless_instance_last_seen_seconds gauge");
                    foreach (var kv in instanceLastSeen)
                    {
                        var secs = new DateTimeOffset(kv.Value).ToUnixTimeSeconds();
                        var instLabel = SanitizeLabelName("instance");
                        var valEscaped = kv.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        sb.AppendLine($"headless_instance_last_seen_seconds{{{instLabel}=\"{valEscaped}\"}} {secs}");
                    }
                    }
                    catch { }

                    var bytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                    res.ContentType = "application/json";
                    res.ContentLength64 = bytes.Length;
                    res.OutputStream.Write(bytes, 0, bytes.Length);
                    res.OutputStream.Close();
                    return;
                }

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/metrics")
                {
                    Console.WriteLine($"[MetricsServer] /metrics requested, {store.Count} metrics stored");
                    var sb = new StringBuilder();

                    // group by metric name to emit HELP/TYPE once
                    var byName = new Dictionary<string, List<MetricEntry>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in store.Values)
                    {
                        if (!byName.TryGetValue(kv.Name, out var list))
                        {
                            list = new List<MetricEntry>();
                            byName[kv.Name] = list;
                        }
                        list.Add(kv);
                    }

                    foreach (var kvp in byName)
                    {
                        var metricName = kvp.Key;
                        sb.AppendLine($"# HELP {metricName} Metric pushed from headless");
                        sb.AppendLine($"# TYPE {metricName} gauge");

                        foreach (var entry in kvp.Value)
                        {
                            var labels = entry.Labels;
                            var labelText = string.Empty;
                            if (labels != null && labels.Count > 0)
                            {
                                var parts = new List<string>(labels.Count);
                                foreach (var l in labels)
                                {
                                    var key = SanitizeLabelName(l.Key);
                                    var valEscaped = l.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                                    parts.Add($"{key}=\"{valEscaped}\"");
                                }
                                labelText = "{" + string.Join(",", parts) + "}";
                            }

                            sb.AppendLine($"{metricName}{labelText} {entry.Value}");
                        }
                    }

                    var outb = Encoding.UTF8.GetBytes(sb.ToString());
                    res.ContentType = "text/plain; version=0.0.4";
                    res.ContentLength64 = outb.Length;
                    res.OutputStream.Write(outb, 0, outb.Length);
                    res.OutputStream.Close();
                    return;
                }

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/instances")
                {
                    try
                    {
                        var ttl = TimeSpan.FromSeconds(90);
                        var now = DateTime.UtcNow;
                        var list = new List<object>();
                        foreach (var kv in instanceLastSeen)
                        {
                            list.Add(new { instance = kv.Key, lastSeen = kv.Value, online = (now - kv.Value) <= ttl });
                        }
                        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(list));
                        res.ContentType = "application/json";
                        res.ContentLength64 = bytes.Length;
                        res.OutputStream.Write(bytes, 0, bytes.Length);
                        res.OutputStream.Close();
                    }
                    catch { }
                    return;
                }

                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/")
                {
                    var bytes = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                    res.ContentType = "application/json";
                    res.ContentLength64 = bytes.Length;
                    res.OutputStream.Write(bytes, 0, bytes.Length);
                    res.OutputStream.Close();
                    return;
                }

                res.StatusCode = 404;
                res.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetricsServer] Error handling request: {ex.Message}");
            }
        }

        private static string CanonicalKey(string name, IDictionary<string, string> labels)
        {
            // produce a stable key: name|k1=v1|k2=v2 sorted by key
            var sb = new StringBuilder();
            sb.Append(name);
            foreach (var kv in new SortedDictionary<string, string>(labels, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append('|');
                sb.Append(kv.Key);
                sb.Append('=');
                sb.Append(kv.Value);
            }
            return sb.ToString();
        }

        private static string SanitizeLabelName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "label";
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if ((i == 0 && (char.IsLetter(c) || c == '_')) || (i > 0 && (char.IsLetterOrDigit(c) || c == '_')))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }
    }
}
