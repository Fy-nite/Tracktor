using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Core.Elements.Designers
{
    public class UIBuilder
    {
        private UIBuilder _instance;
        private Control _container;
        public UIBuilder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UIBuilder();
                }
                return _instance;
            }
        }
        public UIBuilder() { 
            _container = new Control(); // init the root node.
        }
        
        public Control BuildUI()
        {
            return _container;
        }

        public void button(string text, Action? onClick)
        {
            var button = new V12.Core.UI.UITemplates.UIButton();
            button.text = text;
            if (onClick != null)
            {
                button.Pressed += () => onClick();
            }
        }

    }
}
