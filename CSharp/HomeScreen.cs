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

		var ui = GetNode<CanvasLayer>("UI");
		var playDesktopBtn = ui.GetNode<Button>("NetConnect/VBox/Buttons/PlayDesktop");
		var playVRBtn = ui.GetNode<Button>("NetConnect/VBox/Buttons/PlayVR");
		playDesktopBtn.Pressed += OnPlayDesktopPressed;
		playVRBtn.Pressed += OnPlayVRPressed;

		var singlePlayerBtn = ui.GetNode<Button>("NetConnect/VBox/Buttons/SinglePlayer");
		singlePlayerBtn.Pressed += OnSinglePlayerPressed;

		if (MainScene != null)
		{
			// Only load main game if not in networked mode
			if (!IsNetworkedMode())
			{
				mainGame = MainScene.Instantiate();
				AddChild(mainGame);
			}
		}

		_net = GetNodeOrNull<NetRoot>("NetRoot");
		_vrStarted = false;
	}

	private void OnSinglePlayerPressed()
	{
		if (mainGame != null)
			return;
		if (MainScene != null)
		{
			mainGame = MainScene.Instantiate();
			AddChild(mainGame);
			GetNode<CanvasLayer>("UI").Visible = false;
		}
	}

	private void OnPlayDesktopPressed()
	{
		GetTree().ChangeSceneToFile("res://assets/Scenes/MainGame.tscn");
	}

	private void OnPlayVRPressed()
	{
		// Enable XR before loading VR scene
		EnableXRAndLoadVRScene();
	}

	private async void EnableXRAndLoadVRScene()
	{
		var xrStartScene = GD.Load<PackedScene>("res://addons/xr/xr_start.tscn");
		var xrStart = xrStartScene.Instantiate();
		AddChild(xrStart);
		await ToSignal(xrStart, "xr_initialized"); // Assume xr_start.gd emits this signal when ready
		GetTree().ChangeSceneToFile("res://assets/Scenes/VRPlayer.tscn");
	}

	private bool IsNetworkedMode()
	{
		return _net != null && _net.IsInsideTree();
	}
}
