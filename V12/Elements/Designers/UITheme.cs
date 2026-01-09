using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Core.Elements.Designers
{
    public class UITheme
    {
        public static StyleBoxFlat GetDefaultPanelStyle()
        {
            var styleBox = new StyleBoxFlat();
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderColor = new Godot.Color(0.2f, 0.2f, 0.2f);
            styleBox.BgColor = new Godot.Color(0.1f, 0.1f, 0.1f);
            styleBox.CornerRadiusTopLeft = 5;
            styleBox.CornerRadiusTopRight = 5;
            styleBox.CornerRadiusBottomLeft = 5;
            styleBox.CornerRadiusBottomRight = 5;
            styleBox.ExpandMarginBottom = 5;
            return styleBox;
        }
    }
}
