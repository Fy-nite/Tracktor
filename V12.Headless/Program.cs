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
using System.Text.Json;
using V12.Types;
using V12.Interfaces;

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

        // Create a simple headless world instance and register a RequestWorld handler
        var world = new V12.V12World("Headless", headless: true);
        // never use protobuf for any world messaging or serialization.

        // proper JSON handler for RequestWorld, json should be the only format used for world data.
        headless.RegisterJson("RequestWorldJson", async (ep, json) =>
        {
          
            var response = JsonSerializer.Serialize(world);
            Console.WriteLine($"Sending world data to {ep}");
            Console.WriteLine(response);
            await headless.SendJsonAsync(ep, response, "WorldData");
        });

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
        app.MapGet("/RequestWorld", () =>
        {
            return JsonSerializer.Serialize(new Element
            {
                Id = "world_root",
                Name = "World Root",
                Type = "World",
                Description = "The root element of the world",
                ChildElements = new List<IWorldElement>
                {
                    new Element {
                        Id = "element_1",
                        Name = "Sample Element 1",
                        Type = "Environment",
                        Description = "A sample environment element",
                        Position = new DoubleVector3(0,0,0),
                        Rotation = new DoubleVector3(0,0,0),
                        Scale = new DoubleVector3(1,1,1)
                    }
                }
            });
        });

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
