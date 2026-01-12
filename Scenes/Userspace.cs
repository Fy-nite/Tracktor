using Godot;
using System.Collections.Generic;
using V12.Interfaces;
using V12.Types;
using BoxMesh = Tracktor.Components.BoxMesh;

namespace V12.World
{
    public class Userspace
    {
        public Userspace() { }
        public Userspace(string name) { }
        public static Element Generate()
        {
            var userSpaceElement = new Element
            {
                Name = "UserSpaceRoot",
                Description = "Root element for user space",
                ChildElements =
                {
                    new Element
                    {
                        Name = "UserSpaceArea",
                        Description = "An area within user space",
                        ChildElements = {

                        },
                        Components = new List<IComponent>()
                    }
                },
                Components =
                {
                    new BoxMesh
                    {
                        Name = "UserSpaceBoundary",
                        Position = new Vector3(0, 0, 0),
                        Rotation = new Vector3(0, 0, 0),
                        Scale = new Vector3(10, 1, 10),

                    }
                }
            };

            return userSpaceElement;
        }
    }
}
