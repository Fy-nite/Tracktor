using Godot;
using System.Collections.Generic;
using System.Reflection.Metadata;
using V12;
using V12.Components.Test;
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
                    Name = "Desktop player",
                    Components = new List<IComponent>
                    {
                        new V12.Components.DesktopPlayer
                        {
                            Position = new Vector3(0, 5, 0),
                            Rotation = new Vector3(0, 0, 0),
                            Scale = new Vector3(1, 1, 1),
                        }
                    }
                },
                
                new Element
                    {
                        Name = "Light Source",
                        Components = new List<IComponent>
                        {
                            new V12.Components.DirectionalLight
                            {
                                Position = new Vector3(0, 10, 0),
                                Rotation = new Vector3(-90, 0, 0),
                                Scale = new Vector3(1, 1, 1),
                                Energy = 0.5f,
                            }

                        }
                    },
                    new Element {
                        Name = "SkyBox",
                        Components = new List<IComponent>
                        {
                            new V12.Components.PanoramaSky
                            {
                                TexturePath = "res://Assets/Day.hdr",

                                Size = 300.0f,
                            }
                        }
                    },

                    new Element {
                        Name = "Root Inspector",
                        Components = new List<IComponent>
                        {
                            new V12.Components.Inspector
                            {
                                Position = new Vector3(0, 1, 0),
                                Rotation = new Vector3(0, 0, 0),
                                Scale = new Vector3(0.5f, 0.5f, 0.5f)
                            }
                        }
                    },

                    new Element
                    {
                        Name = "EventTesting",
                        Components = new List<IComponent>
                        {
                            new V12.Components.Test.BlinkBox
                            {
                                Position = new Vector3(1, 1, 1),
                                Rotation = new Vector3(0, 0, 0),
                                Scale = new Vector3(1, 1, 1)
                            }
                        },
                        ChildElements = new List<IWorldElement>
                        {
                            new Element
                            {
                                Name = "test",
                                ChildElements = new List<IWorldElement>
                                {
                                    new Element
                                    {
                                        Name = "test2",
                                        Components = new List<IComponent>
                                        {
                                            new V12.Components.Test.BlinkBox
                                            {
                                                Position = new Vector3(1, 2.1f, 1),
                                                Rotation = new Vector3(0, 0, 0),
                                                Scale = new Vector3(1, 1, 1)
                                            },
                                            new V12.Components.Test.EventSender
                                            {
                                                Event = "TestEvent",
                                                Interval = 1
                                            }
                                        }
                                    }
                                }
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
                    }
                }
            };
            world.RootElement.AddChild(WorldFloor);
            // Add Desktop player
            GD.Print("World Initialization Completed");
            return this;
        }
    }
}
