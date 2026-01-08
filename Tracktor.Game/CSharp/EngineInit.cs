#if GODOT
using Godot;
using System.Collections.Generic;
using System.Linq;
using Tracktor.CSharp;
using V12;
using V12.Bindings;
using V12.Components.VR;
using V12.Extensions;
using V12.Interfaces;
using V12.Types;
using V12.Headless;
using V12.Headless.Messages;
using System.Threading.Tasks;
using V12.Types.Models;
using System;
using Pyroscope;
using System.Threading;

public partial class EngineInit : Node3D
{
    public V12Engine engine;
    XRInteractionHandler xrHandler = new XRInteractionHandler();
    public int currentWorldIndex = 0;
    // optional network client for requesting world from server
    V12.Headless.WorldClient? networkClient;
    V12.Headless.WorldServer? localServer;
    // track worlds added by the network so we can remove them on disconnect
    List<V12World> networkWorlds = new();

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
        // register Godot engine bridge so V12 code can call into Godot
        EngineBridgeProvider.Instance = new GodotEngineBridge(this);
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
#else
using System;
using System.Collections.Generic;
using V12;
using V12.Bindings;

// Minimal headless-friendly EngineInit: no Godot dependencies.
public partial class EngineInit
{
    public V12Engine engine;
    public int currentWorldIndex = 0;
    List<V12World> networkWorlds = new();

    // Thread-safe queue for actions that must run on the main thread
    private readonly Queue<Action> _mainThreadActions = new();
    private readonly object _mainThreadLock = new();

    public EngineInit()
    {
        // register a null bridge for headless execution
        EngineBridgeProvider.Instance = new NullEngineBridge();
        engine = new V12Engine();

        var world = new V12World("LocalWorld");
        new WorldInit().InitializeLocalHome(world);
        var userWorld = new V12World("UserSpace");
        engine.AddWorld(world);
        engine.AddWorld(userWorld);

        userWorld.IsLocalOnly = true;
        userWorld.CanBeLeft = true;
        userWorld.CanBeClosed = false;
        world.IsLocalOnly = true;
        world.CanBeLeft = true;
        world.CanBeClosed = false;

        engine.Run();
    }

    public void RunOnMainThread(Action action)
    {
        if (action == null) return;
        lock (_mainThreadLock)
        {
            _mainThreadActions.Enqueue(action);
        }
        // execute immediately in headless mode
        while (_mainThreadActions.Count > 0)
        {
            Action work;
            lock (_mainThreadLock)
            {
                work = _mainThreadActions.Dequeue();
            }
            try { work(); } catch { }
        }
    }
#endif
		//foreach (var item in userWorld.RootElement.ChildElements)
		//{
		//    GD.Print("UserSpace Child Element: " + item.Name);
		//    foreach (var comp in item.Components)
		//    {
		//        GD.Print(" - Component: " + comp.GetType().FullName);
		//    }
		//    foreach (var item2 in item.ChildElements)
		//    {
		//        GD.Print("   - Child Element: " + item2.Name);
		//    }
		//}
		//var inspector = new V12.UI.Inspector(userWorld.RootElement);
		//AddChild(inspector.UIRoot); // Add to the scene as a UI overlay
	   
		engine.Run();

		// create simple UI for connect/disconnect
		CreateNetworkUI();
		
		// connect to server if requested via command line
		try
		{
			var args = OS.GetCmdlineArgs();
			var lower = new List<string>();
			foreach (var a in args) lower.Add(a.ToLowerInvariant());

			// allow starting a local headless server from the game process (use headless world)
			if (lower.Contains("--start-server"))
			{
				int port = 7777;
				int portIdx = lower.IndexOf("--port");
				if (portIdx != -1 && portIdx + 1 < args.Length && int.TryParse(args[portIdx + 1], out var p)) port = p;
				GD.Print($"[EngineInit] Starting local WorldServer on port {port}");

				// build a simple host world with children (headless)
				var hostWorld = V12.World.Hub.CreateHubWorld("HostWorld", headless: true);


				try
				{
					localServer = new V12.Headless.WorldServer(port, () => hostWorld);
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

				networkClient = new V12.Headless.WorldClient(serverAddress, port);
				networkClient.OnFullWorldReceived += (full) =>
				{
					GD.Print($"[Network] Received full world '{full.Name}'");
					// build a V12World from proto
					var remote = new V12World(full.Name ?? "RemoteWorld");
					remote.IsLocalOnly = full.IsLocalOnly;
					remote.CanBeLeft = full.CanBeLeft;
					remote.CanBeClosed = full.CanBeClosed;

					if (full.Root != null)
					{
						// convert the proto root to engine Element
						var rootEl = full.Root.ToElement();
						// copy basic info into the world's existing RootElement
						remote.RootElement.Name = rootEl.Name;
						remote.RootElement.Description = rootEl.Description;
						remote.RootElement.Type = rootEl.Type;
						remote.RootElement.Level = rootEl.Level;
						remote.RootElement.Position = rootEl.Position;
						remote.RootElement.Rotation = rootEl.Rotation;
						remote.RootElement.Scale = rootEl.Scale;

						// attach children and components
						foreach (var child in rootEl.ChildElements)
						{
							remote.RootElement.AddChild(child);
						}
						foreach (var comp in rootEl.Components)
						{
							remote.RootElement.AddComponent(comp);
						}
					}

                    // Enqueue main-thread action to add and activate the remote world
                    RunOnMainThread(() =>
                    {
                        engine.AddWorld(remote, this);
                        engine.ActivateWorld(engine.Worlds.Count - 1, this);
                        var sg = remote.sceneGenerator;
                        if (sg != null)
                        {
                            sg.RegenerateTree();
                        }
                    });

					// subscribe to incremental element updates for this remote world
					var worldName = full.Name ?? "RemoteWorld";
					networkClient.OnElementProtoReceived += (elemProto) =>
					{
                        // Enqueue incremental element handling on the main thread
                        RunOnMainThread(() =>
                        {
                            try
                            {
                                GD.Print($"[Network][Deferred] Incremental element received: {elemProto.Name} for world {worldName}");
                                var element = elemProto.ToElement();
                                var remoteWorld = engine.Worlds.Find(w => w.Name == worldName);
                                if (remoteWorld != null)
                                {
                                    var existing = remoteWorld.RootElement.FindChild(element.Name);
                                    if (existing != null)
                                    {
                                        existing.Description = element.Description;
                                        existing.Type = element.Type;
                                        existing.Level = element.Level;
                                        existing.Position = element.Position;
                                        existing.Rotation = element.Rotation;
                                        existing.Scale = element.Scale;
                                    }
                                    else
                                    {
                                        remoteWorld.RootElement.AddChild(element);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                GD.PrintErr($"[Network][Deferred] Error handling element proto: {ex.Message}");
                            }
                        });
					};
				};

				// start listening and request world
				_ = Task.Run(async () =>
				{
					await networkClient.ConnectAsync();
					await networkClient.RequestWorldAsync();
				});
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
			connectButton.Pressed += () => ConnectToLocalHost();
			vbox.AddChild(connectButton);

			disconnectButton = new Button();
			disconnectButton.Text = "Disconnect";
			disconnectButton.Pressed += () => DisconnectAndUpdateUI();
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
		DisconnectNetworkClient();
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
				if (networkClient != null)
				{
					GD.Print("[EngineInit] Already connected to world server.");
				}
				else
				{
					ConnectToLocalHost();
				}
			}
		}
	}

	// Create and start a WorldClient connected to localhost:7777 and wire handlers
	public void ConnectToLocalHost()
	{
		try
		{
			GD.Print("[EngineInit] Connecting to local world server 127.0.0.1:7777...");
			networkClient = new V12.Headless.WorldClient("127.0.0.1", 7777);
			SetupNetworkHandlers(networkClient);

			_ = System.Threading.Tasks.Task.Run(async () =>
			{
				try
				{
					await networkClient.ConnectAsync();
					await networkClient.RequestWorldAsync();
					GD.Print("[EngineInit] Requested world from local server.");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[EngineInit] Network error: {ex.Message}");
				}
			});
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[EngineInit] Failed to start WorldClient: {ex.Message}");
		}
	}

	public void DisconnectNetworkClient()
	{
		if (networkClient == null && networkWorlds.Count == 0) return;

		// remove any worlds that were added by the network
		try
		{
			foreach (var rw in networkWorlds.ToList())
			{
				try
				{
					// deactivate and remove from scene tree if present
					if (rw.IsActive) rw.Deactivate();
					if (rw.Viewport != null && rw.Viewport.GetParent() == this)
					{
						RemoveChild(rw.Viewport);
					}
					// remove from engine list
					engine.Worlds.Remove(rw);
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Network] Error removing remote world '{rw?.Name}': {ex.Message}");
				}
			}
			networkWorlds.Clear();
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Network] Error cleaning up remote worlds: {ex.Message}");
		}

		// dispose network client
		try
		{
			networkClient?.Dispose();
		}
		catch { }
		networkClient = null;

		// reset UI and current world index
		UpdateNetworkUIConnected(false);
		currentWorldIndex = 0;
	}

	private void SetupNetworkHandlers(V12.Headless.WorldClient client)
	{
		client.OnFullWorldReceived += (full) =>
		{
			GD.Print($"[Network] Received full world '{full.Name}'");
			// build a V12World from proto on a background thread, then enqueue attachment on main thread
			try
			{
				var remote = new V12World(full.Name ?? "RemoteWorld");
				remote.IsLocalOnly = full.IsLocalOnly;
				remote.CanBeLeft = full.CanBeLeft;
				remote.CanBeClosed = full.CanBeClosed;

				if (full.Root != null)
				{
					var rootEl = full.Root.ToElement();
					remote.RootElement.Name = rootEl.Name;
					remote.RootElement.Description = rootEl.Description;
					remote.RootElement.Type = rootEl.Type;
					remote.RootElement.Level = rootEl.Level;
					remote.RootElement.Position = rootEl.Position;
					remote.RootElement.Rotation = rootEl.Rotation;
					remote.RootElement.Scale = rootEl.Scale;

					foreach (var child in rootEl.ChildElements)
					{
						remote.RootElement.AddChild(child);
					}
					foreach (var comp in rootEl.Components)
					{
						remote.RootElement.AddComponent(comp);
					}
				}

				// Enqueue the scene-tree modifications on the main thread
				RunOnMainThread(() =>
				{
				engine.AddWorld(remote, this);
				engine.ActivateWorld(engine.Worlds.Count - 1, this);
				// remember that this world came from the network so we can remove it on disconnect
				networkWorlds.Add(remote);
				// remember the newly activated world as the current index so SwitchWorld works
				currentWorldIndex = engine.Worlds.Count - 1;
				UpdateNetworkUIConnected(true);
					var sg = remote.sceneGenerator;
					if (sg != null) sg.RegenerateTree();
				});
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Network] Error processing full world: {ex.Message}");
			}
		};

			client.OnElementProtoReceived += (elemProto) =>
		{
			// Enqueue incremental element handling on main thread
			RunOnMainThread(() =>
			{
				try
				{
					GD.Print($"[Network][Deferred] Incremental element received: {elemProto.Name}");
					var element = elemProto.ToElement();
				// fallback: pick the first non-local world (excluding LocalWorld)
				var remoteWorld = engine.Worlds.FirstOrDefault(w => !w.IsLocalOnly && w.Name != "LocalWorld");
					if (remoteWorld != null)
					{
						var existing = remoteWorld.RootElement.FindChild(element.Name);
						if (existing != null)
						{
							existing.Description = element.Description;
							existing.Type = element.Type;
							existing.Level = element.Level;
							existing.Position = element.Position;
							existing.Rotation = element.Rotation;
							existing.Scale = element.Scale;
						}
						else
						{
							remoteWorld.RootElement.AddChild(element);
						}
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[Network][Deferred] Error handling element proto: {ex.Message}");
				}
			});
		};
	}

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
