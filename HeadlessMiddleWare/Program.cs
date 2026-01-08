// Headless launcher — starts a headless UDP server and an HTTP management server
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using V12.Headless;
using Microsoft.AspNetCore.Builder;

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

        // Start a simple HTTP management server using HttpListener to avoid bringing ASP.NET packages into the headless app
        var webTask = HeadlessMiddleWare.SimpleHttpServer.StartAsync(httpPort, udpPort, cts.Token);
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
