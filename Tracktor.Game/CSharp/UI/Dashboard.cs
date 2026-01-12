using Godot;
using V12.Types;


namespace Tracktor.Tracktor.Game.CSharp.UI
{
    public class Dashboard
    {
        public static Element Generate()
        {
            Vector3 P = new Vector3(0, 0, 0);
            Vector3 R = new Vector3(0, 0, 0);
            Vector3 S = new Vector3(1, 1, 1);
            var dash = new Element { Name = "Dashboard" };
            var background = new Element
            {
                Name = "Background",
                Scale = new DoubleVector3(1, 1, 1, 1), // Reasonable size for testing
                Position = new DoubleVector3(0, 0, 0, 1)
            };
            var meshComp = new Components.BoxMesh
            {
                Position = P,
                Rotation = R,
                Scale = S
            };
            background.AddComponent(meshComp);
            dash.AddChild(background);
            return dash;
        }
    }
}
