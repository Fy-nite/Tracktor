using Godot;
using System;

public partial class Reticle1 : Reticle
{
	[ExportCategory("Reticle")]
	[ExportGroup("Nodes")]
	[Export]
	Line2D[] reticleLines = new Line2D[4] ;

	[ExportGroup("Animate")]
	[Export]
	bool animatedReticle = true ;
	[Export]
	float reticleSpeed = 0.5f;
	[Export]
	float reticleSpread = 4.0f;

	[ExportGroup("Dot Settings")]
	[Export]
	int dotSize = 1 ;
	[Export]
	Color dotColor = Colors.White ;

	[ExportGroup("Line Settings")]
	[Export]
	Color lineColor = Colors.White ;
	[Export]
	int lineWidth = 2 ;
	[Export]
	int lineLength = 10 ;
	[Export]
	int lineDistance = 5 ;
	[Export(PropertyHint.Enum, "None,Round")]
	int capMode = 0 ;

	public override void _Process(double delta)
	{
		if (Visible)
		{
			updateReticleSettings() ;
			if (animatedReticle)
			{
				animateReticleLines() ;
			}
		}
	}

    private void animateReticleLines()
    {
		Vector3 velocity = character.GetRealVelocity() ;
		Vector3 origin = Vector3.Zero ;
		Vector2 pos = Vector2.Zero ;
		float speed = origin.DistanceTo(velocity) ;
		
		reticleLines[0].Position = reticleLines[0].Position.Lerp(pos + new Vector2(0, -speed * reticleSpread), reticleSpeed) ;
		reticleLines[1].Position = reticleLines[1].Position.Lerp(pos + new Vector2(-speed * reticleSpread, 0), reticleSpeed) ;
		reticleLines[2].Position = reticleLines[2].Position.Lerp(pos + new Vector2(speed * reticleSpread, 0), reticleSpeed) ;
		reticleLines[3].Position = reticleLines[3].Position.Lerp(pos + new Vector2(0, speed * reticleSpread), reticleSpeed) ;
    }

    private void updateReticleSettings()
    {
		Vector2 newScale = new Vector2 (dot.Scale.X, dot.Scale.Y) ;
		dot.Scale = newScale ;

		//Lines
		foreach (Line2D line in reticleLines)
		{
			line.DefaultColor = lineColor ;
			line.Width = lineWidth ;
			line.BeginCapMode = capMode == 0 ? Line2D.LineCapMode.None : Line2D.LineCapMode.Round ;
			line.EndCapMode = capMode == 0 ? Line2D.LineCapMode.None : Line2D.LineCapMode.Round ;
		}
    }
}
