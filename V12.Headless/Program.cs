// Headless launcher — starts a headless UDP server and an HTTP management server
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using V12.Headless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        int udpPort = 7777;
        int httpPort = 5000;

        // allow overriding ports via args: --udp 7777 --http 5000
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--udp" && i + 1 < args.Length && int.TryParse(args[i + 1], out var up)) udpPort = up;
            if (args[i] == "--http" && i + 1 < args.Length && int.TryParse(args[i + 1], out var hp)) httpPort = hp;
        }

        using var cts = new CancellationTokenSource();

        // Start headless UDP server
        var headless = new Server(udpPort);
        headless.Start();
        Console.WriteLine($"Headless UDP server listening on port {udpPort}");

        // Periodically push player metrics to the configured metrics server
        // Include labels so the central metrics server can distinguish instances
        _ = Task.Run(async () =>
        {
            var instance = Environment.MachineName;
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["instance"] = instance,
                        ["udp_port"] = udpPort.ToString(),
                        ["http_port"] = httpPort.ToString()
                    };

                    V12.Headless.Data.MetricsClient.SetConnectedPlayers(headless.GetConnectedClientCount(), labels);
                    // Optionally push world count if available (0 for now)
                    V12.Headless.Data.MetricsClient.SetWorldCount(0, labels);
                }
                catch
                {
                    // ignore
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ContinueWith(_ => { });
            }
        });

        // Start an ASP.NET Core management server
        var builder = WebApplication.CreateBuilder(args);
        // Listen on the configured port
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(httpPort));
        builder.Services.ConfigureOpenTelemetryTracerProvider(telemetry =>
        {
            telemetry.AddAspNetCoreInstrumentation();
            telemetry.AddHttpClientInstrumentation();
            telemetry.SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault());
            telemetry.AddHttpClientInstrumentation();
            telemetry.AddHangfireInstrumentation();
        });
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("V12.Headless.Server")
                    .AddPrometheusExporter();
            });

        var app = builder.Build();

        // serve a simple management UI at /
        app.MapGet("/", () => Results.Content(@"<!doctype html>
<html>
<head>
  <meta charset='utf-8'>
  <title>Headless Management</title>
  <style>body{font-family:Arial,Helvetica,sans-serif;margin:20px}table{border-collapse:collapse}td,th{border:1px solid #ddd;padding:8px}</style>
</head>
<body>
  <h1>Headless Server</h1>
  <div id='status'>Loading...</div>
  <h2>Connected Players</h2>
  <div id='players'>Loading...</div>
  <script>
    async function refresh(){
      try{
        let r = await fetch('/players');
        let j = await r.json();
        document.getElementById('status').textContent = 'HTTP management: online';
        let p = document.getElementById('players');
        p.innerHTML = '<p>Count: ' + j.count + '</p>';
        if (j.endpoints && j.endpoints.length){
          let ul = document.createElement('ul');
          for (let e of j.endpoints){ let li = document.createElement('li'); li.textContent = e; ul.appendChild(li); }
          p.appendChild(ul);
        }
      }catch(e){ document.getElementById('status').textContent = 'Error: ' + e; }
    }
    setInterval(refresh, 2000);
    refresh();
  </script>
</body>
</html>", "text/html"));

        app.MapGet("/players", () =>
        {
            return Microsoft.AspNetCore.Http.Results.Json(new
            {
                count = headless.GetConnectedClientCount(),
                endpoints = headless.GetConnectedClientEndpoints()
            });
        });

        app.MapGet("/health", () => "healthy");
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        //app.MapPost("/shutdown", () => { cts.Cancel(); return new { shuttingDown = true }; });

        var webTask = app.RunAsync();
        Console.WriteLine($"HTTP management server listening on port {httpPort}");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Wait until cancelled
        try
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("Shutting down...");
        try
        {
            await headless.StopAsync().ConfigureAwait(false);
        }
        catch { }
        headless.Dispose();

        // ensure web host finishes
        try { await webTask; } catch { }

        Console.WriteLine("Stopped.");
    }
}
