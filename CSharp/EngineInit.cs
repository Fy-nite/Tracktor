using Godot;
using System.Collections.Generic;
using Tracktor.CSharp;
using V12;
using V12.Components.VR;
using V12.Interfaces;
using V12.Types;

public partial class EngineInit : Node3D
{
    public V12Engine engine;
    VRInteractionController vrInteractionController;
    XRInteractionHandler xrHandler = new XRInteractionHandler();
    public int currentWorldIndex = 0;

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
    public void Initialize()
    {
        var vrNode = GD.Load<PackedScene>("res://V12/Scenes/VR/VRPlayerBody.tscn").Instantiate<Node3D>();
        AddChild(vrNode);
        GD.Print("[VRPlayer] Created VRPlayer Node3D");

        // search inside the instance (recursive) instead of GetTree().Root
        var leftNode = vrNode.FindChild("LeftHand", true, false) as XRController3D;
        var rightNode = vrNode.FindChild("RightHand", true, false) as XRController3D;

        if (leftNode != null)
        {
            leftNode.Connect("button_pressed", new Callable(xrHandler, "OnLeftNodeButtonPressed"));
            leftNode.Connect("input_float_changed", new Callable(xrHandler, "OnLeftNodeFloatChanged"));
            leftNode.Connect("input_vector2_changed", new Callable(xrHandler, "OnLeftNodeVector2Changed"));
            GD.Print("[VRPlayer] Connected LeftHand XRController3D signals");
        }
        else
        {
            GD.Print("[VRPlayer] LeftHand not found under vrNode");
        }

        if (rightNode != null)
        {
            rightNode.Connect("button_pressed", new Callable(xrHandler, "OnRightNodeButtonPressed"));
            rightNode.Connect("input_float_changed", new Callable(xrHandler, "OnRightNodeFloatChanged"));
            rightNode.Connect("input_vector2_changed", new Callable(xrHandler, "OnRightNodeVector2Changed"));
            GD.Print("[VRPlayer] Connected RightHand XRController3D signals");
        }
        else
        {
            GD.Print("[VRPlayer] RightHand not found under vrNode");
        }

        // Subscribe to XRInteractionHandler events (centralized handling)
        xrHandler.ButtonPressed += (hand, button) =>
        {
            GD.Print($"[VRPlayer] {hand} controller pressed {button}");

        };
        xrHandler.TriggerChanged += (hand, value) =>
        {
            GD.Print($"[VRPlayer] {hand} trigger value: {value}");
        };
        xrHandler.StickChanged += (hand, value) =>
        {
            GD.Print($"[VRPlayer] {hand} stick value: {value}");
        };
    }
    public override void _Ready()
    {


        AddToGroup("engine_init");
        Initialize();
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
        userWorld.RootElement.AddChildElement(V12.World.Userspace.Generate());

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
       
        //engine.Run();
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
