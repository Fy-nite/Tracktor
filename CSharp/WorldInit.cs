using Godot;
using System.Collections.Generic;
using V12;
using V12.Interfaces;
using V12.Types;
namespace Tracktor.CSharp
{
    public partial class WorldInit : Node3D
    {
        public Node3D InitializeLocalHome(V12World world)
        {
            GD.Print("World Initialization Started");
            Element WorldFloor = new Element
            {
                Name = "World Floor",
                ChildElements = new List<IWorldElement>
                {
                    new Element
                    {
                        Name = "Light Source",
                        Components = new List<IComponent>
                        {
                            new V12.Components.DirectionalLight
                            {
                                Position = new Vector3(0, 10, 0),
                                Rotation = new Vector3(-45, 0, 0),
                                Scale = new Vector3(1, 1, 1),
                                Name = "Sun Light",
                                Color = new Color(1, 1, 0.9f),
                                Energy = 1.0f,
                                ShadowEnabled = true
                            }
                        }
                    }
                },
                Components = new List<IComponent>
                {
                    new V12.Components.BoxMesh
                    {
                        Position = new Vector3(0, -0.5f, 0),
                        Rotation = new Vector3(0, 0, 0),
                        Scale = new Vector3(100, 1, 100),
                        // Name is set in BoxMesh constructor
                    }
                }
            };
            world.RootElement.AddChildElement(WorldFloor);
            return this;
        }
    }
}
