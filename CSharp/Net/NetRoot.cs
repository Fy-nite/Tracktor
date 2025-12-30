#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Trakction.XR;

public partial class NetRoot : Node
{
	[Export] public int Port = 7777;

	public bool IsServerMode { get; private set; }
	public string RemoteHost { get; private set; } = "127.0.0.1";
	public int PortInUse { get; private set; }
	public string ConnectionStatus { get; private set; } = "Offline";

	private MultiplayerSpawner _spawner = null!;
	private Node3D _spawnRoot = null!;

	public override void _Ready()
	{
		AddToGroup("net_root");

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += () =>
		{
			ConnectionStatus = "Connected";
			GD.Print("Connected to server");
		};
		Multiplayer.ConnectionFailed += () =>
		{
			ConnectionStatus = "ConnectionFailed";
			GD.PrintErr("Connection failed");
		};

		var args = OS.GetCmdlineArgs().ToList();

		var wantsServer = args.Contains("--server");
		var connectIndex = args.IndexOf("--connect");
		var wantsClient = connectIndex >= 0;

		var host = "127.0.0.1";
		var port = Port;

		if (wantsClient && connectIndex + 1 < args.Count)
			host = args[connectIndex + 1];

		var portIndex = args.IndexOf("--port");
		if (portIndex >= 0 && portIndex + 1 < args.Count && int.TryParse(args[portIndex + 1], out var parsedPort))
			port = parsedPort;

		RemoteHost = host;
		PortInUse = port;

		// Important: Only autostart when explicitly requested.
		if (wantsServer)
		{
			IsServerMode = true;
			StartServer(port);
		}
		else if (wantsClient)
		{
			IsServerMode = false;
			StartClient(host, port);
		}
		else
		{
			IsServerMode = false;
			ConnectionStatus = "Offline";
		}

		// Setup Spawner and World Root
		_spawnRoot = GetNodeOrNull<Node3D>("World");
		if (_spawnRoot == null)
		{
			_spawnRoot = new Node3D { Name = "World" };
			AddChild(_spawnRoot);
		}

		_spawner = GetNodeOrNull<MultiplayerSpawner>("MultiplayerSpawner");
		if (_spawner == null)
		{
			_spawner = new MultiplayerSpawner { Name = "MultiplayerSpawner" };
			AddChild(_spawner);
		}

		_spawner.SpawnPath = _spawnRoot.GetPath();
		_spawner.SpawnFunction = new Callable(this, nameof(SpawnNode));

		// Ensure SceneSync exists and is pointed at the same World root so mirror snapshots include world content.
		var sceneSync = GetNodeOrNull<SceneSync>("SceneSync");
		if (sceneSync == null)
		{
			sceneSync = new SceneSync { Name = "SceneSync" };
			AddChild(sceneSync);
		}
		sceneSync.SyncRootPath = _spawnRoot.GetPath();

		// Code for headless server mode
		if (OS.HasFeature("Server"))
		{
			GD.Print("Running in headless server mode.");
			IsServerMode = true;
			StartServer(port);
		}
		else
		{
			// Code for client/GUI mode
			GD.Print("Running in client mode.");
			IsServerMode = false;
			ConnectionStatus = "Offline";
		}
	}

	private Node SpawnNode(Variant data)
	{
		var dict = data.AsGodotDictionary();
		string path = dict.ContainsKey("path") ? dict["path"].AsString() : "";
		
		if (string.IsNullOrEmpty(path))
		{
			GD.PrintErr("SpawnNode: path is empty");
			return new Node();
		}
		
		// Try load
		var scene = GD.Load<PackedScene>(path);
		if (scene == null)
		{
			 GD.PrintErr($"SpawnNode: could not load {path}");
			 return new Node();
		}
		
		var node = scene.Instantiate();
		
		// Apply transform
		if (node is Node3D n3d)
		{
			if (dict.ContainsKey("pos")) n3d.Position = dict["pos"].AsVector3();
			if (dict.ContainsKey("rot")) n3d.Rotation = dict["rot"].AsVector3();
		}
		
		// Apply name
		if (dict.ContainsKey("name")) node.Name = dict["name"].AsString();
		
		// Apply authority
		if (dict.ContainsKey("auth"))
		{
			int authId = dict["auth"].AsInt32();
			node.SetMultiplayerAuthority(authId);
		}

		// Add dynamic synchronizer if requested
		if (dict.ContainsKey("sync_transform") && dict["sync_transform"].AsBool())
		{
			var sync = node.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
			if (sync == null)
			{
				sync = new MultiplayerSynchronizer();
				sync.Name = "MultiplayerSynchronizer";
				node.AddChild(sync);
				
				var config = new SceneReplicationConfig();
				// Note: Properties are relative to the synchronizer's parent (the node)
				config.AddProperty(":position");
				config.AddProperty(":rotation");
				sync.ReplicationConfig = config;
			}
		}
		
		return node;
	}

	public void ServerSpawnEntity(string packedScenePath, Vector3 position)
	{
		if (!Multiplayer.IsServer())
		{
			GD.PrintErr("ServerSpawnEntity: server-only");
			return;
		}

		var data = new Godot.Collections.Dictionary
		{
			{ "path", packedScenePath },
			{ "pos", position },
			{ "sync_transform", true }
		};
		_spawner.Spawn(data);
	}

	public void ServerSpawn(string spawnKey, Vector3 position, Godot.Collections.Dictionary? props = null)
	{
		if (!Multiplayer.IsServer())
		{
			GD.PrintErr("ServerSpawn: server-only");
			return;
		}

		// Assuming spawnKey is a path or mapped to one. For now treating as path or simple name.
		// If it's just "Box", we might need a map.
		string path = spawnKey;
		if (!path.Contains("res://"))
		{
			// Simple mapping for demo
			if (path == "Box") path = "res://assets/Scenes/Box.tscn"; // Example path
			// You might want a better resource lookup here
		}

		var data = new Godot.Collections.Dictionary
		{
			{ "path", path },
			{ "pos", position },
			{ "sync_transform", true }
		};
		
		if (props != null)
		{
			// Merge props if needed, or pass them along
		}

		_spawner.Spawn(data);
	}

	/// <summary>
	/// Server helper to spawn a simple Box primitive above the server's player entity.
    /// Visible action for a server UI button.
    /// </summary>
    public void ServerSpawnBoxAboveSelf()
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ServerSpawnBoxAboveSelf: server-only");
            return;
        }

        var selfId = Multiplayer.GetUniqueId();
		var playerNode = _spawnRoot.GetNodeOrNull($"Player_{selfId}");

        if (playerNode == null || playerNode is not Node3D p3d)
        {
			GD.PrintErr("ServerSpawnBoxAboveSelf: couldn't find player node");
            return;
        }

        var spawnPos = p3d.GlobalPosition + new Vector3(0, 2.0f, 0);

		// Using a generic box scene - ensure this exists or use a primitive
		// Since we want to spawn "anything", let's assume we have a Box.tscn or similar.
		// If not, we can't spawn a primitive via Spawner easily without a scene.
		// For now, I'll assume a path.
		string boxPath = "res://assets/Scenes/Box.tscn"; 

		var data = new Godot.Collections.Dictionary
		{
			{ "path", boxPath },
			{ "pos", spawnPos },
			{ "sync_transform", true }
		};

		_spawner.Spawn(data);
		GD.Print("ServerSpawnBoxAboveSelf: spawned box above server player at " + spawnPos);
    }

    public void ServerDespawnEntity(Node node)
    {
        if (!Multiplayer.IsServer())
        {
			GD.PrintErr("ServerDespawnEntity: server-only");
            return;
        }
		node.QueueFree(); // Spawner handles despawn replication automatically
    }

    public void DebugPrintEntities()
    {
		GD.Print($"Entities in World: {_spawnRoot.GetChildCount()}");
        foreach (Node node in _spawnRoot.GetChildren())
        {
			if (node is Node3D n3d)
				GD.Print($"  {node.Name} ({node.SceneFilePath}) pos={n3d.GlobalPosition}");
        }
    }

    public void StartServerFromUi(int port)
    {
        StopNetworking();
        IsServerMode = true;
        PortInUse = port;
        StartServer(port);
    }

    public void StartClientFromUi(string host, int port)
    {
        StopNetworking();
        IsServerMode = false;
        RemoteHost = host;
        PortInUse = port;
        StartClient(host, port);
    }

    public void StopNetworking()
    {
        try
        {
            if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enet)
                enet.Close();
        }
        catch (Exception ex)
        {
			GD.PrintErr($"StopNetworking error: {ex.Message}");
        }

        Multiplayer.MultiplayerPeer = null;
		ConnectionStatus = "Offline";

        // Clear spawned entities
        foreach (Node child in _spawnRoot.GetChildren())
        {
            child.QueueFree();
        }
        
        // If we were a client, also clear the main game scene children so the 3D world is removed on disconnect.
        // Since we refractored, nuke Maingame as our scene hirarchy root is different now.
        try
        {
            if (!IsServerMode)
            {
                var root = GetTree()?.Root;
                if (root != null)
                {
					var mainGame = root.GetNodeOrNull<Node>("MainGame");
                    if (mainGame != null)
                    {
                        mainGame.QueueFree();
						GD.Print("StopNetworking: freed MainGame node.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
			GD.PrintErr($"StopNetworking cleanup error: {ex.Message}");
        }
    }

    private void StartServer(int port)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port, maxClients: 32);
        Multiplayer.MultiplayerPeer = peer;
		ConnectionStatus = "Listening";
		GD.Print($"Server listening on {port}");

        // Spawn server's own player (peerId=1) so you can test in non-headless too.
        if (_spawner != null)
            ServerEnsurePlayer(1);
    }

    private void StartClient(string host, int port)
    {
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(host, port);
        Multiplayer.MultiplayerPeer = peer;
		ConnectionStatus = "Connecting";
		GD.Print($"Client connecting to {host}:{port}");
    }

    private void OnPeerConnected(long peerId)
    {
		GD.Print($"Peer connected: {peerId}");

        if (!Multiplayer.IsServer())
            return;

        if (_spawner == null)
            return;

        // Ensure server-side player entity exists.
        ServerEnsurePlayer(peerId);
    }

    private void OnPeerDisconnected(long peerId)
    {
		GD.Print($"Peer disconnected: {peerId}");

        if (Multiplayer.IsServer())
        {
            var node = _spawnRoot.GetNodeOrNull($"Player_{peerId}");
            if (node != null)
            {
                node.QueueFree(); // Spawner handles despawn replication automatically
            }
        }
    }

    private void ServerEnsurePlayer(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;
        if (_spawner == null)
            return;
        
        string name = $"Player_{peerId}";
        if (_spawnRoot.HasNode(name))
            return;

        // IMPORTANT: peerId is not guaranteed to be small/contiguous (it can be a large random-ish value).
        // Never derive spawn positions from peerId.
        var spawnIndex = _spawnRoot.GetChildCount();
        var spawnPos = new Vector3(spawnIndex * 1.5f, 0, 0);
        
        var data = new Godot.Collections.Dictionary
        {
            { "path", "res://assets/Scenes/NetPlayer.tscn" },
            { "pos", spawnPos },
            { "name", name },
            { "auth", (int)peerId },
            { "sync_transform", true }
        };
        
        var node = _spawner.Spawn(data);
        GD.Print($"ServerEnsurePlayer: spawned {node.Name} for peer {peerId} at {spawnPos}");

        if (node is PlayerAvatar avatar)
        {
            avatar.PeerId = peerId;
            avatar.IsLocalPlayer = (Multiplayer.MultiplayerPeer != null && peerId == Multiplayer.GetUniqueId());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void ServerStartVr(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;

		GD.Print($"Peer {peerId} started VR mode");

        // Despawn the regular player avatar
        var oldNode = _spawnRoot.GetNodeOrNull($"Player_{peerId}");
        if (oldNode != null)
        {
            oldNode.QueueFree();
        }

        // Spawn the VR player rig
        var spawnIndex = _spawnRoot.GetChildCount();
        var spawnPos = new Vector3(spawnIndex * 1.5f, 0, 0);
        
        var data = new Godot.Collections.Dictionary
        {
            { "path", "res://assets/Scenes/NetVRPlayer.tscn" },
            { "pos", spawnPos },
            { "name", $"Player_{peerId}" },
            { "auth", (int)peerId },
            { "sync_transform", false }
        };
        
        var node = _spawner.Spawn(data);

        if (node is XrPlayerRig rig)
        {
            rig.PeerId = peerId;
            rig.IsLocalPlayer = (Multiplayer.MultiplayerPeer != null && peerId == Multiplayer.GetUniqueId());
        }
    }

    public void StartVr()
    {
        if (Multiplayer.MultiplayerPeer == null)
            return;
        if (Multiplayer.IsServer())
        {
            ServerStartVr(Multiplayer.GetUniqueId());
            return; 
        }
        RpcId(1, nameof(ServerStartVr), Multiplayer.GetUniqueId());
    }
}
