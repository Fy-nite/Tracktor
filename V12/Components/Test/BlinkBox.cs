using Godot;
using System.Collections.Generic;
using V12.Types;

namespace V12.Components.Test
{
	public class BlinkBox : NodeComponent
	{
		[InspectorExpose]
		public Vector3 Position { get; set; }
		[InspectorExpose]
		public Vector3 Rotation { get; set; }

		[InspectorExpose]
		public Vector3 Scale { get; set; }
		[InspectorExpose]
		public List<string> strings { get; set; } = new List<string>();

		public override string Category => "Mesh";
		public override string Description => "A simple box mesh";

		public string MeshType => "Box";
		
		private bool red = false;
		public Mesh mesh;
		StandardMaterial3D mat;
		public BlinkBox()
		{
			Name = "Box";
		}
		public BlinkBox(Vector3 position, Vector3 rotation, Vector3 scale) : base()
		{
			Position = position;
			Rotation = rotation;
			Scale = scale;
		}
		public override Control GenerateUI()
		{
			return ComponentUIGenerator.GenerateAutoUI(this);
		}

		public override void OnAwake()
		{
			if (sceneElement == null)
			{
				mesh = new Godot.BoxMesh
				{
					Size = Scale
				};
				var meshInstance = new MeshInstance3D
				{
					Mesh = mesh,
					Position = Position,
					Rotation = Rotation,
					Name = Name
				};
				sceneElement = meshInstance;
				mat = new StandardMaterial3D();
				mat.AlbedoColor = Colors.Red;
				mesh.SurfaceSetMaterial(0, mat);
			}
		}
		public override void OnAttach()
		{
			attachedElement?.eventManager.registerTreeEvent<GenericEvent>("TestEvent", (e) => UpdateColor(), this, attachedElement);
			base.OnAttach();
		}
		void UpdateColor()
		{
			GD.Print($"Got event, {attachedElement?.Name}");
			mat.AlbedoColor = (red ^= true) ? Colors.Red : Colors.Blue;
		}
	}
}
