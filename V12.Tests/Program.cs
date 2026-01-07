
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
        Console.WriteLine($"[Client] Connecting to chat server at {serverAddress}:{port}");
        using var client = new ChatClient(serverAddress, port);
        using var shutdown = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };
        client.OnChatReceived += msg => Console.WriteLine($"[{msg.User}] {msg.Text}");
        client.OnElementReceived += elem => Console.WriteLine($"[Element] {elem.Name} ({elem.Type}) lvl {elem.Level}");
        await client.ConnectAsync();
        Console.WriteLine("[Client] Connected. Type messages to send, or 'exit' to quit.");
        var inputTask = Task.Run(async () =>
        {
            while (!shutdown.Token.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null || line.ToLowerInvariant() == "exit")
                {
                    shutdown.Cancel();
                    break;
                }
                await client.SendChatMessageAsync("User", line);
            }
        });
        try
        {
            await Task.Delay(Timeout.Infinite, shutdown.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[Client] Shutting down...");
        }
        await inputTask;
    }
    private static async Task RunServerAsync(int port)
    {
        Console.WriteLine($"[Server] Starting chat server on port {port} (Ctrl+C to exit)");

        using var host = new ChatHost(port);
        using var shutdown = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            shutdown.Cancel();
        };

        host.OnChatReceived += msg => Console.WriteLine($"[{msg.User}] {msg.Text}");
        host.OnElementReceived += elem => Console.WriteLine($"[Element] {elem.Name} ({elem.Type}) lvl {elem.Level}");

        host.Start();

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
