using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace V12.Headless.Data
{
    /// <summary>
    /// Lightweight metrics helper for headless process.
    /// - exposes observable instruments via System.Diagnostics.Metrics for in-process collectors
    /// - pushes metrics (with optional labels) to a standalone HTTP metrics server (default http://localhost:9000/push-metrics)
    /// </summary>
    internal static class MetricsClient
    {
        private static readonly Meter _meter = new("V12.Headless", "1.0");

        // internal state
        private static int _connectedPlayers;
        private static int _worldCount;
        private static long _messagesReceived;
        private static long _messagesSent;
        private static readonly DateTime _start = DateTime.UtcNow;

        // instruments
        private static readonly ObservableGauge<int> _connectedPlayersGauge;
        private static readonly ObservableGauge<int> _worldCountGauge;
        private static readonly ObservableGauge<double> _uptimeGauge;
        private static readonly Counter<long> _messagesReceivedCounter;
        private static readonly Counter<long> _messagesSentCounter;

        private static readonly HttpClient _http = new();
        private static readonly string _metricsServerUrl = Environment.GetEnvironmentVariable("METRICS_SERVER_URL") ?? "http://localhost:9000";

        static MetricsClient()
        {
            _connectedPlayersGauge = _meter.CreateObservableGauge<int>("headless_connected_players", ObserveConnectedPlayers, description: "Number of connected clients to this headless server");
            _worldCountGauge = _meter.CreateObservableGauge<int>("headless_world_count", ObserveWorldCount, description: "Number of worlds loaded in this headless server");
            _uptimeGauge = _meter.CreateObservableGauge<double>("headless_uptime_seconds", ObserveUptime, description: "Time since process start in seconds");

            _messagesReceivedCounter = _meter.CreateCounter<long>("headless_messages_received");
            _messagesSentCounter = _meter.CreateCounter<long>("headless_messages_sent");
        }

        private static IEnumerable<Measurement<int>> ObserveConnectedPlayers()
        {
            yield return new Measurement<int>(Volatile.Read(ref _connectedPlayers));
        }

        private static IEnumerable<Measurement<int>> ObserveWorldCount()
        {
            yield return new Measurement<int>(Volatile.Read(ref _worldCount));
        }

        private static IEnumerable<Measurement<double>> ObserveUptime()
        {
            yield return new Measurement<double>((DateTime.UtcNow - _start).TotalSeconds);
        }

        /// <summary>
        /// Update the current connected players count. Optionally provide labels for this metric.
        /// </summary>
        public static void SetConnectedPlayers(int count, IDictionary<string, string>? labels = null)
        {
            Interlocked.Exchange(ref _connectedPlayers, count);
            _ = PushAsync(new[] { ToMetricPayload("headless_connected_players", count, labels) });
        }

        /// <summary>
        /// Set the number of worlds.
        /// </summary>
        public static void SetWorldCount(int count, IDictionary<string, string>? labels = null)
        {
            Interlocked.Exchange(ref _worldCount, count);
            _ = PushAsync(new[] { ToMetricPayload("headless_world_count", count, labels) });
        }

        /// <summary>
        /// Increment messages received counter (and push current counters).
        /// </summary>
        public static void IncrementMessagesReceived(IDictionary<string, string>? labels = null)
        {
            Interlocked.Increment(ref _messagesReceived);
            _messagesReceivedCounter.Add(1);
            _ = PushAsync(new[] { ToMetricPayload("headless_messages_received", Volatile.Read(ref _messagesReceived), labels) });
        }

        /// <summary>
        /// Increment messages sent counter (and push current counters).
        /// </summary>
        public static void IncrementMessagesSent(IDictionary<string, string>? labels = null)
        {
            Interlocked.Increment(ref _messagesSent);
            _messagesSentCounter.Add(1);
            _ = PushAsync(new[] { ToMetricPayload("headless_messages_sent", Volatile.Read(ref _messagesSent), labels) });
        }

        private static object ToMetricPayload(string name, object value, IDictionary<string, string>? labels)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            dict["name"] = name;
            dict["value"] = value;
            if (labels != null)
            {
                dict["labels"] = labels;
            }
            // always include instance
            dict.TryAdd("instance", Environment.MachineName);
            return dict;
        }

        private static async Task PushAsync(object[] metrics)
        {
            try
            {
                var payload = new Dictionary<string, object>
                {
                    { "metrics", metrics }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await _http.PostAsync(new Uri(new Uri(_metricsServerUrl), "/push-metrics"), content).ConfigureAwait(false);
            }
            catch
            {
                // best-effort, ignore errors
            }
        }

        /// <summary>
        /// Return a snapshot of basic headless status for UI or HTTP endpoints.
        /// </summary>
        public static Dictionary<string, object> GetStatus()
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["connectedPlayers"] = Volatile.Read(ref _connectedPlayers),
                ["worldCount"] = Volatile.Read(ref _worldCount),
                ["messagesReceived"] = Volatile.Read(ref _messagesReceived),
                ["messagesSent"] = Volatile.Read(ref _messagesSent),
                ["uptimeSeconds"] = (DateTime.UtcNow - _start).TotalSeconds,
                ["instance"] = Environment.MachineName
            };

            return dict;
        }
    }
}
