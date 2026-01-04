using Godot;
using V12.Extensions;
using V12.Interfaces;
using V12.Types;
using V12.UI;

namespace V12.Components
{
    public class Inspector: NodeComponent
    {
        [InspectorExpose]
        public Vector3 Position { get; set; } = new Vector3(0,0,0);
        [InspectorExpose]
        public Vector3 Rotation { get; set; } = new Vector3(0,0,0);

        [InspectorExpose]
        public Vector3 Scale { get; set; } = new Vector3(1,1,1);

        public IWorldElement Root { get; set; }

        public override string Category => "Mesh";
        public override string Description => "A simple box mesh";

        public string MeshType => "Box";
        public Inspector()
        {
            // defer creation of sceneElement until RunComponentLogic so object initializer values are applied
        }
        public Inspector(Vector3 position, Vector3 rotation, Vector3 scale, IWorldElement Root)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            this.Root = Root;
        }
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        public override void OnAwake()
        {
            if (sceneElement == null)
            {
                var inspectorUI = new UI.Inspector(this.attachedElement);
                var SubViewport = new Godot.SubViewport();
                // put the UI root inside the viewport
                SubViewport.AddChild(inspectorUI);
                // match viewport size to UI so the texture looks correct
                SubViewport.Size = (Vector2I)inspectorUI.UIRoot.Size;

                var InspectorMesh = new MeshInstance3D();
                var quadSize = inspectorUI.UIRoot.Size / 200f; // scale down for 3D world
                InspectorMesh.Mesh = new Godot.QuadMesh
                {
                    Size = quadSize
                };

                InspectorMesh.RotationDegrees = Rotation;
                InspectorMesh.Scale = Scale;


                var node3D = new Node3D
                {
                    Name = "InspectorContainer"
                };

                // add the viewport to the container so it will render when the container is added to the scene
                node3D.AddChild(SubViewport);

                // create a backing quad slightly larger than the UI quad so the UI has a solid background
                var backSize = new Vector2(quadSize.X + 0.05f, quadSize.Y + 0.05f);
                var backMesh = new MeshInstance3D();
                backMesh.Mesh = new Godot.QuadMesh
                {
                    Size = backSize
                };
                // position the backing slightly behind the UI quad in local space (parented to InspectorMesh)
                backMesh.Position = new Vector3(0f, 0f, -0.01f);
                backMesh.RotationDegrees = Vector3.Zero;
                backMesh.Scale = Vector3.One;

                // simple dark material for the backing
                var backMaterial = new StandardMaterial3D();
                backMaterial.AlbedoColor = new Color(0.08f, 0.08f, 0.08f);
                backMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // show both sides
                backMesh.SetSurfaceOverrideMaterial(0, backMaterial);

                // obtain the viewport's texture and assign to the mesh material
                var material = new StandardMaterial3D();
                var viewportTexture = SubViewport.GetTexture();
                material.AlbedoTexture = viewportTexture;
                InspectorMesh.MaterialOverride = material;
                InspectorMesh.MaterialOverlay = material;
                InspectorMesh.SetSurfaceOverrideMaterial(0, material);

                // add the backing and title as children of InspectorMesh
                InspectorMesh.AddChild(backMesh);

                // add the 3D title above the quad (local position so it follows the mesh)
                var titleText = this.attachedElement?.Name ?? "Inspector";
                var title = new Label3D
                {       
                    Name = "InspectorTitle3D",
                    Text = titleText
                };
                var yOffset = (quadSize.Y / 2f) + 0.1f;
                title.Position = new Vector3(0f, yOffset, 0f);
                title.RotationDegrees = Vector3.Zero;
                title.Scale = new Vector3(0.03f, 0.03f, 0.03f);
                InspectorMesh.AddChild(title);

                // add the mesh after the viewport has been added to the container
                node3D.AddChild(InspectorMesh);
                node3D.Position = Position;
                sceneElement = node3D;
            }
        }
    }
}
