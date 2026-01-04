using Godot;
using Tracktor.CSharp;
using V12;

public partial class EngineInit : Node3D
{
    public V12Engine engine;
    public int currentWorldIndex = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        AddToGroup("engine_init");
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
            var VRPlayer = new V12.Components.VR.VRPlayer().GenerateVRPlayer();
            AddChild(VRPlayer); // Add VRPlayer to the scene, and make sure the player can be used all worlds
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
        foreach (var item in userWorld.RootElement.ChildElements)
        {
            GD.Print("UserSpace Child Element: " + item.Name);
            foreach (var comp in item.Components)
            {
                GD.Print(" - Component: " + comp.GetType().FullName);
            }
            foreach (var item2 in item.ChildElements)
            {
                GD.Print("   - Child Element: " + item2.Name);
            }
        }
        var inspector = new V12.UI.Inspector(userWorld.RootElement);
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
