using Godot;
using System;

public partial class Reticle : CenterContainer
{
	[ExportCategory("Reticle")]
	[ExportGroup("Nodes")]
	[Export]
	public CharacterBody3D character {get; set; }

	public Polygon2D dot ;

    public override void _Ready()
    {
		dot = GetNode<Polygon2D>("dot") ;
    }

}
