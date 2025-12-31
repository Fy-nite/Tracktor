using Godot;
using System;
using V12;
using V12.UI;
using V12.Components.VR;
using V12.Types;
using V12.Components;
public partial class EngineInit : Node3D
{
	V12Engine engine;
	int currentWorldIndex = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		engine = new V12Engine();

		var mainWorldRoot = new Node3D();
		AddChild(mainWorldRoot);
		var world = new V12World("MainWorld");
		engine.Worlds.Add(world);
		mainWorldRoot.AddChild(world.Viewport);
		world.GenerateWorld();

		// Spawn a VR player in MainWorld
		var vrPlayer = new Element { Name = "VRPlayer" };
		vrPlayer.AddComponent(new VROriginComponent());
		vrPlayer.AddComponent(new VRCameraComponent { HeightOffset = 1.7f });
		var leftHand = new VRControllerComponent { Hand = false };
		var rightHand = new VRControllerComponent { Hand = true };
		leftHand.Name = "LeftHandController";
		rightHand.Name = "RightHandController";
		//leftHand.AddSubComponent(new V12.Components.BoxMesh(new Vector3(0.5f,0.5f,0.5f)));
		//rightHand.AddSubComponent(new V12.Components.BoxMesh(new Vector3(0.5f, 0.5f, 0.5f)));
        // add the hands to the vrPlayer
		var leftHandElement = new Element { Name = "LeftHand" };
		leftHandElement.AddComponent(leftHand);
		leftHandElement.AddComponent(new V12.Components.BoxMesh(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f)));

        var rightHandElement = new Element { Name = "RightHand" };
		rightHandElement.AddComponent(rightHand);
		rightHandElement.AddComponent(new V12.Components.BoxMesh(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f)));

		vrPlayer.AddChildElement(leftHandElement);
		vrPlayer.AddChildElement(rightHandElement);



        world.RootElement.AddChildElement(vrPlayer);

		var world2Root = new Node3D();
		AddChild(world2Root);
		var world2 = new V12World("World2");
		engine.Worlds.Add(world2);
		world2Root.AddChild(world2.Viewport);
		world2.GenerateWorld();

		var userWorldRoot = new Node3D();
		AddChild(userWorldRoot);
		var userWorld = new V12World("UserSpace");
		engine.Worlds.Add(userWorld);
		userWorld.IsLocalOnly = true;
		userWorld.CanBeLeft = true;
		userWorld.CanBeClosed = false;
		userWorld.RootElement.AddChildElement(V12.UI.Dashboard.Generate()); // Only here!
		userWorldRoot.AddChild(userWorld.Viewport);
		foreach (var w in engine.Worlds)
		{
			GD.Print("World added: " + w.Name);
        }
		foreach (var item in userWorld.RootElement.ChildElements)
		{
			GD.Print("UserSpace Child Element: " + item.Name);
			foreach (var comp in item.Components)
			{
				GD.Print(" - Component: " + comp.Name);
            }
			foreach (var item2 in item.ChildElements)
			{
				GD.Print("   - Child Element: " + item2.Name);
            }
        }
        var inspector = new V12.UI.Inspector(world.RootElement);
        AddChild(inspector.UIRoot); // Add to the scene as a UI overlay

        engine.Run();
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

	private void SwitchWorld()
	{
		if (engine.Worlds.Count <= 1) return;

		var currentWorld = engine.Worlds[currentWorldIndex];
		if (!currentWorld.CanBeLeft) return; // Cannot switch away from this world

		// Remove current viewport if not UserSpace
		if (currentWorld.Name != "UserSpace")
		{
			RemoveChild(currentWorld.Viewport);
		}

		// Cycle to next world
		currentWorldIndex = (currentWorldIndex + 1) % engine.Worlds.Count;
		var nextWorld = engine.Worlds[currentWorldIndex];
		AddChild(nextWorld.Viewport);

		// Bring UserSpace to front
		var userWorld = engine.Worlds.Find(w => w.Name == "UserSpace");
		if (userWorld != null)
		{
			MoveChild(userWorld.Viewport, GetChildCount() - 1);
		}
	}
}
