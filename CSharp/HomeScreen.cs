using Godot;
using OCRuntime;
using System;
using System.Reflection;


public partial class HomeScreen : Node
{
	[Export] public PackedScene XrPlayerRigScene { get; set; }
	
	[Export] public string StartVrAction { get; set; } = "ui_accept";
	[Export] public CodeEdit CodeEditor { get; set; }
    /// <summary>
    /// The main scene that gets initialized (e.g., the game world)
    /// </summary>
    [Export] public PackedScene MainScene { get; set; }
	public static string DemoCode; 
	public static IRRuntime Runtime { get; set; }

    private Node mainGame;
	private NetRoot? _net;
	private bool _vrStarted;

	public override void _Ready()
	{
		DemoCode = CodeEditor.Text;
        
        if (MainScene != null)
		{
			// Instance and add the main game scene
			mainGame = MainScene.Instantiate();
			FindChild("SceneSync").AddChild(mainGame);
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
        if (Input.IsKeyPressed(Key.F1))
        {
            GD.Print(CodeEditor.Text.Trim());
            if (Runtime == null)
            {
                Runtime = new IRRuntime(CodeEditor.Text.Trim(), IRRuntime.InputFormat.TextIr);
				new Traktor.Core.IRBindings.GodotBindings().Register(Runtime);
				
				Runtime.OnException += (ex) =>
				{
					GD.PrintErr($"Runtime Exception: {ex.Message}");
					// Optionally show in-game UI, e.g., display error message
					var errorLabel = GetNode<Label>("UI/ColorRect/ErrorList");
					if (errorLabel != null)
					{
						errorLabel.Text = $"Error: {ex.Message}\n";
						errorLabel.Visible = true;
					}
				};
				Runtime.OnStep += (CallInfo, instruction ) =>
				{
					// Optionally log or display step information
					GD.Print($"Runtime Step: ({instruction.opCode}, {instruction.operand}");
					GD.Print($" at {CallInfo.Method} (CallStack Count: {CallInfo.EvalStack.Count})");
					foreach (var item in CallInfo.EvalStack)
					{
						GD.Print($"  Stack Item: {item}");
					}
					GD.Print("----");
					GD.Print($" Current Method Arguments:");
					foreach (var item in CallInfo.Args)
					{
						GD.Print($"  Arg: {item}");
					}
                };
            }
            else
            {
                Runtime.LoadProgram(CodeEditor.Text.Trim(), IRRuntime.InputFormat.TextIr);
            }
            Runtime.Run();
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
				Input.MouseMode = ui.Visible ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
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
