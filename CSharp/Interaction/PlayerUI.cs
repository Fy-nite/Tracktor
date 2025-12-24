using Godot;
using System;

public partial class PlayerUI : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		var ui = GetNode<CanvasLayer>("UI");
		ui.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
