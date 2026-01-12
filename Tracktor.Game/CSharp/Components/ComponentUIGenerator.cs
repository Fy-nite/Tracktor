using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;
namespace V12.Components
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InspectorExposeAttribute : Attribute { }

    public static class ComponentUIGenerator
    {
        public static Control GenerateAutoUI(object component)
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 6);

            // Title with background
            var titlePanel = new PanelContainer();
            titlePanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            var titlePanelStyle = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.3f) };
            titlePanel.AddThemeStyleboxOverride("panel", titlePanelStyle);

            var type = component.GetType();
            var title = new Label
            {
                Text = type.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 24)
            };
            title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            titlePanel.AddChild(title);
            vbox.AddChild(titlePanel);

            // Map types to colors for the ColorRect
            var typeColors = new Dictionary<Type, Color>
            {
                { typeof(string), new Color(0.8f, 0.8f, 1.0f) },
                { typeof(int), new Color(1.0f, 0.8f, 0.8f) },
                { typeof(float), new Color(0.8f, 1.0f, 0.8f) },
                { typeof(double), new Color(0.8f, 1.0f, 0.8f) },
                { typeof(Godot.Vector3), new Color(1.0f, 0.5f, 0.7f) },
                { typeof(List<string>), new Color(0.6f, 0.6f, 1.0f) },
            };

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!Attribute.IsDefined(prop, typeof(InspectorExposeAttribute))) continue;
                if (!prop.CanRead || !prop.CanWrite) continue;

                var hbox = new HBoxContainer();
                var color = typeColors.TryGetValue(prop.PropertyType, out var c) ? c : new Color(0.7f, 0.7f, 0.7f);
                var colorRect = new ColorRect { Color = color, CustomMinimumSize = new Vector2(16, 16) };
                hbox.AddChild(colorRect);
                hbox.AddChild(new Label { Text = prop.Name, ClipContents = false });

                if (prop.PropertyType == typeof(string))
                {
                    var lineEdit = new LineEdit { Text = prop.GetValue(component)?.ToString() ?? "" };
                    lineEdit.TextChanged += newText => prop.SetValue(component, newText);
                    hbox.AddChild(lineEdit);
                }
                else if (prop.PropertyType == typeof(int))
                {
                    var spin = new SpinBox { MinValue = int.MinValue, MaxValue = int.MaxValue, Value = (int)prop.GetValue(component) };
                    spin.ValueChanged += val => prop.SetValue(component, (int)val);
                    hbox.AddChild(spin);
                }
                else if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
                {
                    var spin = new SpinBox { MinValue = -100000, MaxValue = 100000, Step = 0.01, Value = Convert.ToDouble(prop.GetValue(component)) };
                    spin.ValueChanged += val => prop.SetValue(component, Convert.ChangeType(val, prop.PropertyType));
                    hbox.AddChild(spin);
                }
                else if (prop.PropertyType == typeof(Godot.Vector3))
                {
                    var vec = (Godot.Vector3)prop.GetValue(component);
                    var xSpin = new SpinBox { MinValue = -100000, MaxValue = 100000, Step = 0.01, Value = vec.X };
                    var ySpin = new SpinBox { MinValue = -100000, MaxValue = 100000, Step = 0.01, Value = vec.Y };
                    var zSpin = new SpinBox { MinValue = -100000, MaxValue = 100000, Step = 0.01, Value = vec.Z };
                    xSpin.ValueChanged += val => prop.SetValue(component, new Godot.Vector3((float)val, (float)ySpin.Value, (float)zSpin.Value));
                    ySpin.ValueChanged += val => prop.SetValue(component, new Godot.Vector3((float)xSpin.Value, (float)val, (float)zSpin.Value));
                    zSpin.ValueChanged += val => prop.SetValue(component, new Godot.Vector3((float)xSpin.Value, (float)ySpin.Value, (float)val));
                    hbox.AddChild(new Label { Text = "X" }); hbox.AddChild(xSpin);
                    hbox.AddChild(new Label { Text = "Y" }); hbox.AddChild(ySpin);
                    hbox.AddChild(new Label { Text = "Z" }); hbox.AddChild(zSpin);
                }
                else if (prop.PropertyType == typeof(List<string>))
                {
                    var list = (List<string>)prop.GetValue(component) ?? new List<string>();
                    var textEdit = new TextEdit { Text = string.Join("\n", list), SizeFlagsVertical = Control.SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 60) };
                    textEdit.TextChanged += () =>
                    {
                        var lines = textEdit.Text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        prop.SetValue(component, new List<string>(lines));
                    };
                    hbox.AddChild(textEdit);
                }

                // Wrap the row in a panel container so it participates properly in VBox layout
                var rowPanel = new PanelContainer();
                rowPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                var rowStyle = new StyleBoxFlat
                {
                    BgColor = new Color(0.15f, 0.15f, 0.15f),
                    ContentMarginLeft = 6,
                    ContentMarginRight = 6,
                    ContentMarginTop = 4,
                    ContentMarginBottom = 4
                };
                rowPanel.AddThemeStyleboxOverride("panel", rowStyle);
                hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                rowPanel.AddChild(hbox);

                vbox.AddChild(rowPanel);
            }

            // Wrap the whole thing in a PanelContainer for overall background
            var mainPanel = new PanelContainer();
            mainPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            var mainStyle = new StyleBoxFlat
            {
                BgColor = new Color(0.1f, 0.1f, 0.1f),
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 8,
                ContentMarginBottom = 8
            };
            mainPanel.AddThemeStyleboxOverride("panel", mainStyle);
            mainPanel.AddChild(vbox);

            return mainPanel;
        }
    }
}
