
using System.Threading;
using V12.Headless;

namespace V12.Tests;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("V12.Tests - Test Runner and Chat Server");
        var argList = new List<string>();
        foreach (var arg in args)
        {
            argList.Add(arg.ToLowerInvariant());
        }
        Console.WriteLine("Arguments: " + string.Join(" ", argList));

        if (argList.Contains("--run-tests"))
        {
            RunAllTests();
            return 0;
        }
        else if (argList.Contains("--run-client"))
        {
            string serverAddress = "127.0.0.1";
            // default address
            int port = 7777; // default port
            int addressIndex = argList.IndexOf("--server");
            if (addressIndex != -1 && addressIndex + 1 < argList.Count)
            {
                serverAddress = args[addressIndex + 1];
            }
            int portIndex = argList.IndexOf("--port");

            if (portIndex != -1 && portIndex + 1 < argList.Count && int.TryParse(argList[portIndex + 1], out int parsedPort))
            {
                port = parsedPort;
            }
            await RunClientAsync(serverAddress, port);
            return 0;
        }


        else if (argList.Contains("--start-server"))
        {
            int port = 7777; // default port
            int portIndex = argList.IndexOf("--port");
            if (portIndex != -1 && portIndex + 1 < argList.Count && int.TryParse(argList[portIndex + 1], out int parsedPort))
            {
                port = parsedPort;
            }
            await RunServerAsync(port);
            return 0;
        }
        else
        {
            Console.WriteLine("No valid arguments provided. Use --run-tests to run tests or --start-server to start the chat server.");
            return 1;
        }

    }

    private static void RunAllTests()
    {
        Console.WriteLine("[Tests] RunAllTests placeholder - integrate xUnit runner or custom harness here.");
    }
    private static async Task RunClientAsync(string serverAddress, int port)
    {
        Console.WriteLine($"[Client] Connecting to world server at {serverAddress}:{port}");
        using var client = new WorldClient(serverAddress, port);
        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        client.OnFullWorldReceived += world =>
        {
            Console.WriteLine($"[Client] Full world received: {world.Name}");
            try
            {
                var json = world.Root != null ? Google.Protobuf.JsonFormatter.Default.Format(world) : "(no root)";
                Console.WriteLine($"[Client] World JSON: {json}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] JSON format failed: {ex.Message}");
            }
        };

        client.OnElementProtoReceived += elem => Console.WriteLine($"[Client] Incremental element received: {elem.Name}");

        await client.ConnectAsync();
        await client.RequestWorldAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, shutdown.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[Client] Shutting down...");
        }
    }
        private static async Task RunServerAsync(int port)
        {
            Console.WriteLine($"[Server] Starting chat server on port {port} (Ctrl+C to exit)");

            // Build a custom world to host instead of relying on WorldInit
            // Use headless=true so no Godot engine types are constructed in this process
            var hostWorld = new V12.V12World("HostedWorld", headless: true);
            // create a small element tree
            var root = hostWorld.RootElement;
            root.Name = "root world"; // explicit name to match incoming observations
            var childA = new V12.Types.Element("ChildA") { Description = "First child" };
            var childB = new V12.Types.Element("ChildB") { Description = "Second child" };
            var grand = new V12.Types.Element("GrandChild") { Description = "Nested child" };
            childA.AddChild(grand);
            root.AddChild(childA);
            root.AddChild(childB);

            using var host = new WorldServer(port, () => hostWorld);
            using var shutdown = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                shutdown.Cancel();
            };

            // WorldServer will serve the provided hostWorld. Chat events are not available on WorldServer.

            Console.WriteLine("Hosted world built and server started. Press Ctrl+C to stop.");

            try
            {
                await Task.Delay(Timeout.Infinite, shutdown.Token);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[Server] Shutting down...");
            }
        }
}
