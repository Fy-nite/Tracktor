#nullable enable
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

/// <summary>
/// Minimal server-side REPL for headless builds.
///
/// Reads stdin on a background thread and executes commands on the main thread.
///
/// Commands:
/// - help
/// - spawn <alias|res://path.tscn> [x y z]
/// - despawn <entityId>
/// - list
/// - peers
/// - quit
/// </summary>
public sealed partial class ServerRepl : Node
{
    private readonly ConcurrentQueue<string> _pendingLines = new();
    private Thread? _thread;

    // Define aliases for common scenes. Add your .tscn paths here.
    private readonly System.Collections.Generic.Dictionary<string, string> _sceneAliases = new()
    {
        { "box", "res://Scenes/Box.tscn" },
        { "sphere", "res://Scenes/Sphere.tscn" },
        
    };
// define aliases for common Godot built-in scenes or actions as a replacement for tscn paths
    private readonly Dictionary<string, Func<Node>> _GodotAliasses = new()
    {
        {"box", () => new CsgBox3D()},
        {"sphere", () => new CsgSphere3D()},
    };

    public override void _Ready()
    {
        // Only start if we actually have a console.
        // If stdin isn't available, ReadLine() will return null immediately.
        _thread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "ServerRepl"
        };
        _thread.Start();

        GD.Print("Server REPL ready. Type 'help' for commands.");
    }

    public override void _Process(double delta)
    {
        while (_pendingLines.TryDequeue(out var line))
            ExecuteLine(line);
    }

    private void ReadLoop()
    {
        try
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line == null)
                    return;

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                _pendingLines.Enqueue(line);
            }
        }
        catch
        {
            // Ignore stdin issues.
        }
    }

    private void ExecuteLine(string line)
    {
        var parts = SplitArgs(line);
        if (parts.Length == 0)
            return;

        var cmd = parts[0].ToLowerInvariant();
        var net = GetNodeOrNull<NetRoot>("../NetRoot") ?? GetNodeOrNull<NetRoot>("/root/HomeScreen/NetRoot");

        switch (cmd)
        {
			case "loaddll":
				if (parts.Length < 2)
				{
					GD.Print("Usage: loaddll <filepath>");
					return;
				}
				var filepath = parts[1];
				try
				{
					var asm = System.Reflection.Assembly.LoadFrom(filepath);
					foreach (var type in asm.GetTypes())
					{
						GD.Print($"Type: {type.FullName}");
					}
					GD.Print($"Loaded assembly: {asm.FullName}");
				}
				catch (Exception ex)
				{
					GD.Print($"Failed to load assembly from {filepath}: {ex.Message}");
				}
				return;
            case "help":
            case "?":
                GD.Print("Commands:");
                GD.Print("  help");
                GD.Print("  spawn-scene <alias|res://path.tscn> [x y z]");
                GD.Print("  spawn-godot <alias> [x y z]");
                GD.Print("  loaddll <filepath>");
                GD.Print("  despawn <entityId>");
                GD.Print("  list");
                GD.Print("  peers");
                GD.Print("  quit");
                GD.Print("Aliases: box, sphere (add more in _sceneAliases)");
                return;

            case "quit":
            case "exit":
                GD.Print("Quitting...");
                net?.StopNetworking();
                GetTree().Quit();
                return;

            case "peers":
                if (Multiplayer.MultiplayerPeer == null)
                {
                    GD.Print("No multiplayer peer.");
                    return;
                }

                GD.Print($"UniqueId={Multiplayer.GetUniqueId()} IsServer={Multiplayer.IsServer()}");
                foreach (var peerId in Multiplayer.GetPeers())
                    GD.Print($"Peer: {peerId}");
                return;

            case "list":
                if (net == null)
                {
                    GD.Print("NetRoot not found.");
                    return;
                }

                net.DebugPrintEntities();
                return;

            case "despawn":
                if (net == null)
                {
                    GD.Print("NetRoot not found.");
                    return;
                }

                if (parts.Length < 2 || !int.TryParse(parts[1], out var entityId))
                {
                    GD.Print("Usage: despawn <entityId>");
                    return;
                }

                // net.ServerDespawnEntity(entityId);
                return;
            case "spawn-godot":
                if (net == null)
                {
                    GD.Print("NetRoot not found.");
                    return;
                }

                if (parts.Length < 2)
                {
                    GD.Print("Usage: spawn-godot <alias> [x y z]");
                    return;
                }

                var godotAlias = parts[1].ToLowerInvariant();
                if (!_GodotAliasses.TryGetValue(godotAlias, out var nodeFactory))
                {
                    GD.Print($"Unknown Godot alias: {godotAlias}");
                    return;
                }

                var godotPos = Vector3.Zero;
                if (parts.Length >= 5 &&
                    TryParseFloat(parts[2], out var gx) &&
                    TryParseFloat(parts[3], out var gy) &&
                    TryParseFloat(parts[4], out var gz))
                {
                    godotPos = new Vector3(gx, gy, gz);
                }

                var godotNode = nodeFactory();
                GetTree().Root.AddChild(godotNode);
                GD.Print($"Spawned Godot node of type {godotNode.GetType().Name} at pos={godotPos}");
                return;
            
            case "spawn-scene":
                if (net == null)
                {
                    GD.Print("NetRoot not found.");
                    return;
                }

                if (parts.Length < 2)
                {
                    GD.Print("Usage: spawn <alias|res://path.tscn> [x y z]");
                    return;
                }

                var sceneInput = parts[1];
                var scenePath = _sceneAliases.TryGetValue(sceneInput.ToLowerInvariant(), out var aliasPath)
                    ? aliasPath
                    : sceneInput;
                var pos = Vector3.Zero;
                if (parts.Length >= 5 &&
                    TryParseFloat(parts[2], out var x) &&
                    TryParseFloat(parts[3], out var y) &&
                    TryParseFloat(parts[4], out var z))
                {
                    pos = new Vector3(x, y, z);
                }

                net.ServerSpawnEntity(scenePath, pos);
                GD.Print($"Spawned scene '{scenePath}' at {pos}");
                return;

            default:
                GD.Print($"Unknown command: {cmd}. Type 'help'.");
                return;
        }
    }

    private static bool TryParseFloat(string s, out float value)
        => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static string[] SplitArgs(string line)
    {
        // Minimal shell-like parser with quotes.
        var result = new System.Collections.Generic.List<string>();
        var current = "";
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    result.Add(current);
                    current = "";
                }
                continue;
            }

            current += c;
        }

        if (current.Length > 0)
            result.Add(current);

        return result.ToArray();
    }
}
