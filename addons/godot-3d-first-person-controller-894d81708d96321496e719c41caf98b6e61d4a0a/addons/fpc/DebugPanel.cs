using Godot;
using System;

public partial class DebugPanel : PanelContainer
{
	VBoxContainer boxContainer ;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		boxContainer = GetNode<VBoxContainer>("MarginContainer/VBoxContainer") ;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void AddProperty(String title, String value, int order)
	{
		if (boxContainer.GetType()==typeof(VBoxContainer))
		{
			Node target = boxContainer.FindChild(title, true, false) ;
			if (target == null)
			{
				Label label = new Label() ;
				boxContainer.AddChild(label) ;
				label.Name = title ;
				label.Text = $"{title}: {value}" ;
			}
			else if (Visible)
			{
				Label label = (Label)target ;
				label.Text = $"{title}: {value}" ;
				boxContainer.MoveChild(target, order) ;
			}
		}
	}
}
