using Godot;
using System;
using System.Threading.Tasks;
using V12.Headless;

[Tool]
public partial class EditorConnect : EditorScript
{
	// Run this script from the Godot Script Editor (Tools -> Script -> Run) while in the editor.
	// It will connect to a local WorldServer on 127.0.0.1:7777 and request the world.
	public override void _Run()
	{
		GD.Print("[EditorConnect] Starting connection to local host server...");

		try
		{
			var client = new WorldClient("127.0.0.1", 7777);
			client.OnFullWorldReceived += (world) =>
			{
				GD.Print($"[EditorConnect] Full world received: {world?.Name}");
				try
				{
					var json = System.Text.Json.JsonSerializer.Serialize(world);
					GD.Print($"[EditorConnect] World JSON: {json}");
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[EditorConnect] JSON format failed: {ex.Message}");
				}
			};
			client.OnElementReceived += (elem) =>
			{
				GD.Print($"[EditorConnect] Incremental element received: {elem?.Name}");
			};

			// Start listener and request world on a background task so the editor UI doesn't block
			_ = Task.Run(async () =>
			{
				try
				{
					await client.ConnectAsync();
					await client.RequestWorldAsync();
					GD.Print("[EditorConnect] RequestWorld sent");
					
				}
				catch (Exception ex)
				{
					GD.PrintErr($"[EditorConnect] Network error: {ex.Message}");
				}
			});
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[EditorConnect] Failed to start client: {ex.Message}");
		}
	}
}
