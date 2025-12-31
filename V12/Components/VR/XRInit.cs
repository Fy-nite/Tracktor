using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Components.VR
{
    public partial class XRInit : Node3D
    {
        public XRInit() { }
        public static void XRenable()
        {
            GD.Print("XR ENABLED");
            var xrInterface = Godot.XRServer.FindInterface("OpenXR");
            if (xrInterface != null && xrInterface.Initialize())
            {
                Godot.XRServer.SetPrimaryInterface(xrInterface);
                GD.Print("XR Interface initialized and set as primary.");
            }
            else
            {
                GD.PrintErr("Failed to initialize XR Interface.");
            }
        }
    }
}
