using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using V12.Headless.GUI;

namespace Tracktor.CSharp
{
    public partial class Headless : Control
    {
        public override void _Ready()
        {
            AddChild(HeadlessController.GenerateUI()); // this is pretty much it.
        }
    }
}
