using Godot;
using System.Collections.Generic;
using Tracktor.CSharp;
using V12;
using V12.Components.VR;
using V12.Extensions;
using V12.Interfaces;
using V12.Types;
using V12.Headless;
using V12.Headless.Messages;
using System.Threading.Tasks;
using V12.Types.Models;

public partial class EngineInit : Node3D
{
	public V12Engine engine;
	XRInteractionHandler xrHandler = new XRInteractionHandler();
	public int currentWorldIndex = 0;
    // optional network client for requesting world from server
    ChatClient? networkClient;

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


	public override void _Ready()
	{


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
		userWorld.RootElement.AddChild(V12.World.Userspace.Generate());

		foreach (var w in engine.Worlds)
		{
			GD.Print("World added: " + w.Name);
		}
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
		
		// connect to server if requested via command line
		try
		{
			var args = OS.GetCmdlineArgs();
			var lower = new List<string>();
			foreach (var a in args) lower.Add(a.ToLowerInvariant());
			if (lower.Contains("--connect-server"))
			{
				string serverAddress = "127.0.0.1";
				int port = 7777;
				int serverIdx = lower.IndexOf("--server");
				if (serverIdx != -1 && serverIdx + 1 < args.Length) serverAddress = args[serverIdx + 1];
				int portIdx = lower.IndexOf("--port");
				if (portIdx != -1 && portIdx + 1 < args.Length && int.TryParse(args[portIdx + 1], out var p)) port = p;

				networkClient = new ChatClient(serverAddress, port);
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

					// add to engine and activate
					engine.AddWorld(remote, this);
					engine.ActivateWorld(engine.Worlds.Count - 1, this);

					// regenerate the scene for the new world
					var sg = remote.sceneGenerator;
					if (sg != null)
					{
						sg.RegenerateFull();
					}

					// subscribe to incremental element updates for this remote world
					var worldName = full.Name ?? "RemoteWorld";
					networkClient.OnElementProtoReceived += (elemProto) =>
					{
						GD.Print($"[Network] Incremental element received: {elemProto.Name} for world {worldName}");
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
		if (engine.IsEngineRunning)
		{
			engine.Update(delta);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Tab)
			{
				SwitchWorld();
			}
		}
	}

	public void SwitchWorld()
	{
		if (engine.Worlds.Count <= 1) return;

		// compute next index and try to activate it using the engine helper
		int nextIndex = (currentWorldIndex + 1) % engine.Worlds.Count;

		// Ensure current world can be left
		var currentWorld = engine.Worlds[currentWorldIndex];
		if (!currentWorld.CanBeLeft) return;

		engine.ActivateWorld(nextIndex, this);
		currentWorldIndex = nextIndex;

		// Bring UserSpace to front if present
		var userWorld = engine.Worlds.Find(w => w.Name == "UserSpace");
		if (userWorld != null && userWorld.Viewport.GetParent() == this)
		{
			MoveChild(userWorld.Viewport, GetChildCount() - 1);
		}
	}
}
