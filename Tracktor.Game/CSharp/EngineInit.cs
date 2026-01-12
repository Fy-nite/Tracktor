
using Godot;
using System.Collections.Generic;
using Tracktor.CSharp;
using V12;
using V12.Components.VR;
using System;

using System.Threading;

public partial class EngineInit : Node3D
{
    public V12Engine engine;
    XRInteractionHandler xrHandler = new XRInteractionHandler();
    public int currentWorldIndex = 0;
    // network manager (handles connecting/disconnecting and remote worlds)
    NetworkManager? networkManager;

    // simple runtime UI
    CanvasLayer? uiLayer;
    Label? statusLabel;
    Button? connectButton;
    Button? disconnectButton;

    // Called when the node enters the scene tree for the first time.
    public void RecursePrint(Node node)
    {
        GD.Print(node.Name);
        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                RecursePrint(childNode);
            }
        }
    }

    // Thread-safe queue for actions that must run on the main thread
    private readonly Queue<Action> _mainThreadActions = new();
    private readonly object _mainThreadLock = new();

    // Enqueue an action from background threads to be executed on the main thread
    public void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (_mainThreadLock)
        {
            _mainThreadActions.Enqueue(action);
        }
    }


    public override void _Ready()
    {
        GD.Print("Engine Init");

        AddToGroup("engine_init");
        AddChild(V12.Components.VR.VRPlayer.Generate()); // Add VRPlayer to the scene, and make sure the player can be used all worlds (AT THE ROOT!)
        engine = new V12Engine();

        var world = new V12World("LocalWorld");
        new WorldInit().InitializeLocalHome(world);
        var userWorld = new V12World("UserSpace");

        // Add worlds through the engine (activation decides parenting/visibility)
        engine.AddWorld(world);
        engine.AddWorld(userWorld);
        // are we on android without VR support?
        // let's hope we get eventual Android Support.
        if (OS.GetName() == "Android")
        {
            var AndroidPlayer = new V12.Components.Android.AndroidPlayer().GenerateAndroidPlayer();
            AddChild(AndroidPlayer); // Add AndroidPlayer to the scene, and make sure the player can be used all worlds
        }
        else
        {
            //var VRPlayer = new V12.Components.VRControllerHandler.VRPlayer();
            //AddChild(VRPlayer); // Add VRPlayer to the scene, and make sure the player can be used all worlds
            //vrInteractionController = new(VRPlayer);

        }
        // Activate the main world so it's visible, and also activate the local UserSpace so it remains visible
        engine.ActivateWorld(0, this);
        userWorld.Activate(this);

        userWorld.IsLocalOnly = true;
        userWorld.CanBeLeft = true;
        userWorld.CanBeClosed = false;
        //userWorld.RootElement.AddChild(Tracktor.CSharp.Userspace());
        world.IsLocalOnly = true;
        world.CanBeLeft = true;
        world.CanBeClosed = false;

        foreach (var w in engine.Worlds)
        {
            GD.Print("World added: " + w.Name);
        }


        engine.Run();

        // create simple UI for connect/disconnect
        CreateNetworkUI();

        // create network manager and wire callbacks
        networkManager = new NetworkManager(engine, RunOnMainThread, UpdateNetworkUIConnected, this);
        networkManager.OnWorldAdded += (remote, idx) =>
        {
            currentWorldIndex = idx;
            var sg = remote.sceneGenerator;
            if (sg != null) sg.RegenerateTree();
        };
        // subscribe to connection events for immediate UI feedback
        networkManager.OnConnected += () =>
        {
            try { GD.Print("[EngineInit] NetworkManager signaled connected"); } catch { }
            UpdateNetworkUIConnected(true);
        };
        networkManager.OnDisconnected += () =>
        {
            try { GD.Print("[EngineInit] NetworkManager signaled disconnected"); } catch { }
            UpdateNetworkUIConnected(false);
        };

        // connect to server or start local server if requested via command line
        try
        {
            var args = OS.GetCmdlineArgs();
            var lower = new List<string>();
            foreach (var a in args) lower.Add(a.ToLowerInvariant());

            // if the GUI build is started with --start-server, launch a local WorldServer or if headless build
            if (lower.Contains("--start-server") || OS.HasFeature("headless_build"))
            {
                int port = 7777;
                int portIdx = lower.IndexOf("--port");
                if (portIdx != -1 && portIdx + 1 < args.Length && int.TryParse(args[portIdx + 1], out var p)) port = p;
                GD.Print($"[EngineInit] Starting local WorldServer on port {port}");

                var hostWorld = V12.World.Hub.CreateHubWorld("HostWorld", headless: true);
                try
                {
                    networkManager.StartLocalServer(port, () => hostWorld);
                    GD.Print($"[EngineInit] Started local WorldServer on port {port}");
                    if (statusLabel != null) statusLabel.Text = $"Server: running ({port})";
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[EngineInit] Failed to start local server: {ex.Message}");
                }
            }

            if (lower.Contains("--connect-server"))
            {
                string serverAddress = "127.0.0.1";
                int port = 7777;
                int serverIdx = lower.IndexOf("--server");
                if (serverIdx != -1 && serverIdx + 1 < args.Length) serverAddress = args[serverIdx + 1];
                int portIdx = lower.IndexOf("--port");
                if (portIdx != -1 && portIdx + 1 < args.Length && int.TryParse(args[portIdx + 1], out var p)) port = p;

                networkManager.ConnectToAddress(serverAddress, port);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[EngineInit] Network error: {ex.Message}");
        }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Execute any actions queued by background threads that need to run on the main thread
		while (true)
		{
			Action? work = null;
			lock (_mainThreadLock)
			{
				if (_mainThreadActions.Count > 0) work = _mainThreadActions.Dequeue();
			}
			if (work == null) break;
			try { work(); } catch (Exception ex) { GD.PrintErr($"[MainThread] Deferred action error: {ex.Message}"); }
		}

		if (engine.IsEngineRunning)
		{
			engine.Update(delta);
		}
	}

	private void CreateNetworkUI()
	{
		try
		{
			uiLayer = new CanvasLayer();
			AddChild(uiLayer);

			var margin = new MarginContainer();
			margin.AnchorLeft = 0.0f;
			margin.AnchorTop = 0.0f;
			margin.AnchorRight = 0.0f;
			margin.AnchorBottom = 0.0f;
			uiLayer.AddChild(margin);

			var vbox = new VBoxContainer();
			margin.AddChild(vbox);

			statusLabel = new Label();
			statusLabel.Text = "Network: disconnected";
			vbox.AddChild(statusLabel);

			connectButton = new Button();
			connectButton.Text = "Connect (F12)";
			connectButton.Pressed += () =>
			{
				try { GD.Print("[EngineInit] Connect button pressed"); } catch { }
				networkManager?.ConnectToLocalHost();
			};
			vbox.AddChild(connectButton);

			disconnectButton = new Button();
			disconnectButton.Text = "Disconnect";
			disconnectButton.Pressed += () =>
			{
				try { GD.Print("[EngineInit] Disconnect button pressed"); } catch { }
				networkManager?.Disconnect();
				UpdateNetworkUIConnected(false);
			};
			disconnectButton.Disabled = true;
			vbox.AddChild(disconnectButton);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[UI] Failed to create network UI: {ex.Message}");
		}
	}

	private void UpdateNetworkUIConnected(bool connected)
	{
		if (statusLabel != null) statusLabel.Text = connected ? "Network: connected" : "Network: disconnected";
		if (connectButton != null) connectButton.Disabled = connected;
		if (disconnectButton != null) disconnectButton.Disabled = !connected;
	}

	private void DisconnectAndUpdateUI()
	{
		networkManager?.Disconnect();
		UpdateNetworkUIConnected(false);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Tab)
			{
				SwitchWorld();
			}

			// Press F12 to connect to a local host world server (127.0.0.1:7777)
			if (keyEvent.Keycode == Key.F12)
			{
				networkManager?.ConnectToLocalHost();
			}
		}
	}

	// Network methods moved to NetworkManager

	public void SwitchWorld()
	{
		if (engine.Worlds == null || engine.Worlds.Count <= 1) return;

		// try to find the actual current active world index
		int activeIndex = engine.Worlds.FindIndex(w => w.IsActive);
		if (activeIndex == -1)
		{
			activeIndex = currentWorldIndex;
			if (activeIndex < 0 || activeIndex >= engine.Worlds.Count) activeIndex = 0;
		}

		// choose next index, prefer worlds that can be left/switched to
		int nextIndex = (activeIndex + 1) % engine.Worlds.Count;
		for (int i = 0; i < engine.Worlds.Count; i++)
		{
			var idx = (activeIndex + 1 + i) % engine.Worlds.Count;
			if (idx == activeIndex) continue; // skip same
			var cand = engine.Worlds[idx];
			if (cand.CanBeLeft || !engine.Worlds[activeIndex].IsActive)
			{
				nextIndex = idx;
				break;
			}
		}

		var currentWorld = engine.Worlds[activeIndex];
		if (!currentWorld.CanBeLeft)
		{
			GD.Print($"[SwitchWorld] Current world '{currentWorld.Name}' cannot be left");
			return;
		}

		engine.ActivateWorld(nextIndex, this);
		currentWorldIndex = nextIndex;

		// Bring UserSpace to front if present and has a viewport
		var userWorld = engine.Worlds.Find(w => w.Name == "UserSpace");
		if (userWorld != null && userWorld.Viewport != null && userWorld.Viewport.GetParent() == this)
		{
			MoveChild(userWorld.Viewport, GetChildCount() - 1);
		}
	}
}
