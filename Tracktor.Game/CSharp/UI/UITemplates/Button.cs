using System;
using System.Collections.Generic;
using System.Text;
using V12.Core.Interfaces;

namespace Tracktor.Tracktor.Game.CSharp.UI.UITemplates
{
    public class UIButton : IUIElement
    {
        public string text { get; set; } = "Button";
        public Action? OnPressed { get; set; }
        public virtual Godot.Button Draw()
        {
            
            var button = new Godot.Button();
            button.Text = text;
            button.Pressed += () =>
            {
                OnPressed?.Invoke();
            };
            return button;
        }
    }
}
