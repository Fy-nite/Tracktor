// Headless launcher — starts a headless UDP server and an HTTP management server
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using V12.Headless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
        });
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            });

        var app = builder.Build();

        app.MapGet("/", () => new { status = "ok", udpPort });
        app.MapGet("/health", () => "healthy");
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        app.MapPost("/shutdown", () => { cts.Cancel(); return new { shuttingDown = true }; });

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
