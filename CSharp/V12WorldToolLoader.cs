using Godot;
using System;
using V12;
using V12.Extensions;

namespace Tracktor.CSharp
{
	[Tool]
	public partial class V12WorldToolLoader : Node3D
	{
	  

		private Node3D _visualizedWorld;

		public override void _Ready()
		{
			if (Engine.IsEditorHint())
			{
				LoadWorld();
			}
		}

		public override void _Notification(int what)
		{
			
		}

		private void LoadWorld()
		{
			// Remove previous visualization
			if (_visualizedWorld != null)
			{
				RemoveChild(_visualizedWorld);
				_visualizedWorld.QueueFree();
				_visualizedWorld = null;
			}

			// Instantiate and visualize the V12World
			var world = new V12World("World");
			var worldInit = new WorldInit();
			world.RootElement.AddChildElement(worldInit.InitializeLocalHome(world));

			// Use SceneGenerator to build the scene tree
			world.sceneGenerator.RegenerateTree();

			// Store and add the generated Viewport for visualization
			_visualizedWorld = world.Viewport;
			AddChild(_visualizedWorld);
		}
	}
}
