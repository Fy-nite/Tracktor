using Godot;


public partial class HomeScreen : Node
{
    [Export] public PackedScene XrPlayerRigScene { get; set; }
    
    [Export] public string StartVrAction { get; set; } = "ui_accept";
    /// <summary>
    /// The main scene that gets initialized (e.g., the game world)
    /// </summary>
    [Export] public PackedScene MainScene { get; set; }

    private Node mainGame;
    private NetRoot? _net;
    private bool _vrStarted;

    public override void _Ready()
    {
        if (MainScene != null)
        {
            // Instance and add the main game scene
            mainGame = MainScene.Instantiate();
            AddChild(mainGame);
        }
        
        _net = GetNode<NetRoot>("NetRoot");

        // Connect VR button signal
        var vrButton = GetNode<Button>("UI/NetConnect/VBox/Buttons/VrButton");
        vrButton.Pressed += OnVrButtonPressed;

        // Connect Spawn Box button (server-only action)
        var spawnBtn = GetNode<Button>("UI/NetConnect/VBox/Buttons/SpawnBox");
        spawnBtn.Pressed += OnSpawnBoxPressed;
        // Only show spawn button when running as server
        spawnBtn.Visible = _net != null && _net.IsServerMode;
        
        var ui = GetNode<CanvasLayer>("UI");
        ui.Visible = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsKeyPressed(Key.Alt))
        {
            var newScene = ResourceLoader.Load<PackedScene>("res://Assets/Scenes/Dashboard.tscn");
            if (newScene != null)
            {
                GetTree().ChangeSceneToPacked(newScene);
            }
        }
        // Toggle UI / physical panel with Escape (ui_cancel)
        if (@event.IsActionPressed("ui_cancel"))
        {
           
            // Toggle canvas UI visibility
            var ui = GetNode<CanvasLayer>("UI");
            ui.Visible = !ui.Visible;
            
            // Try to find the spawner toggle node in the main game and call its toggle method
            var spawnerToggle = mainGame.FindChild("SpawnerToggle", true, false);
            if (spawnerToggle != null)
            {
                // Call the GDScript method 'toggle_panel' if available
                if (spawnerToggle.HasMethod("toggle_panel"))
                    spawnerToggle.Call("toggle_panel");
                else if (spawnerToggle.HasMethod("open_panel") && ui.Visible == false)
                    spawnerToggle.Call("open_panel");
                else if (spawnerToggle.HasMethod("close_panel") && ui.Visible == true)
                    spawnerToggle.Call("close_panel");
            }
       
        }

        if (_vrStarted)
            return;

        if (@event.IsActionPressed(StartVrAction))
        {
            // Avoid triggering while typing into a LineEdit
            // var focus = GetFocusOwner();
            // if (focus is LineEdit)
            //     return;

            OnVrButtonPressed();
        }
    }

    private void OnVrButtonPressed()
    {
        if (_vrStarted)
            return;
        _vrStarted = true;

        // Always use the networked method to spawn the VR player
        _net?.StartVr();

        // Optionally hide UI or transition
        GetNode<CanvasLayer>("UI").Visible = false;
    }

    // Remove local instantiation logic; spawning is now handled by NetRoot
    private void OnXrEnabledHandler()
    {
        // No-op: VR player spawning is handled by NetRoot and network events
    }

    private void OnSpawnBoxPressed()
    {
        if (_net == null)
            return;

        // Ask the server instance to spawn a box above its player.
        _net.ServerSpawnBoxAboveSelf();
    }
}