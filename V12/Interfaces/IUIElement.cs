using Godot;
using System;
using System.Collections.Generic;
using System.Text;

using V12.Core.Elements.Designers;
using V12.Interfaces;

namespace V12.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a user interface element that can be drawn onto a control.
    /// </summary>
    /// <remarks>Implementations of this interface provide mechanisms to render UI elements within a graphical
    /// user interface. The interface supports both creating a new control for rendering and drawing onto an existing
    /// control. Implementers should ensure that drawing operations are performed in a manner compatible with the
    /// underlying UI framework.</remarks>
    public interface IUIElement 
    {
        public static StyleBoxFlat _theme = UITheme.GetDefaultPanelStyle();
        public virtual Control Draw() {
            return new Control();
        }
        public virtual void Draw(Control control)
        {
            // Default implementation does nothing, act on the provided control
        }
    }
}
