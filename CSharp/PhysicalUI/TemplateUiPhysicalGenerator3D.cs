#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using Traktion.PhysicalUI;

namespace Trakction.PhysicalUI;

/// <summary>
/// Runtime "physical UI" generator that populates a layout matching
/// assets/Scenes/template_ui.tscn by duplicating its existing physical rows.
///
/// This is intentionally minimal: it doesn't attempt to implement full scrolling,
/// layout engines, or input focus. It focuses on:
/// - generating physical list rows (left hierarchy)
/// - generating physical key/value rows (right content)
/// - wiring close + toggle hitboxes via PhysicalHitBoxButton3D
/// </summary>
public partial class TemplateUiPhysicalGenerator3D : Node3D
{
    [Signal] public delegate void ClosedEventHandler();

    [Signal] public delegate void LeftItemToggledEventHandler(int index, bool active);

    [Export] public NodePath RightComponentPath { get; set; } = new("Root/Right Side Components/Scroll_backing/LeftSideScroll/Component");
    [Export] public NodePath RightTitleTextPath { get; set; } = new("Root/Right Side Components/Scroll_backing/LeftSideScroll/Component/TitleText");
    [Export] public NodePath RightContentPath { get; set; } = new("Root/Right Side Components/Scroll_backing/LeftSideScroll/Component/Content");
    [Export] public NodePath CloseButtonPath { get; set; } = new("Root/Right Side Components/Scroll_backing/LeftSideScroll/Component/Content/Closebutton");

    [Export] public NodePath LeftListPath { get; set; } = new("Root/LefttSide Hiericy/Scroll_backing/RightSideScroll");
    [Export] public NodePath LeftItemTemplatePath { get; set; } = new("Root/LefttSide Hiericy/Scroll_backing/RightSideScroll/Item");

    [Export] public NodePath BottomPathTextPath { get; set; } = new("Root/TitleText");

    [Export] public float LeftItemSpacing { get; set; } = 0.155f;
    [Export] public float RightRowSpacing { get; set; } = 0.25f;

    /// <summary>
    /// When true, generator will remove previously generated rows on each Set* call.
    /// </summary>
    [Export] public bool ClearGeneratedOnSet { get; set; } = true;

    private Node3D? _leftList;
    private Node3D? _leftTemplate;

    private Node3D? _rightComponent;
    private Node3D? _rightContent;

    private MeshInstance3D? _rightTitleText;
    private MeshInstance3D? _bottomPathText;

    private PhysicalHitBoxButton3D? _closeButton;

    private readonly List<Node> _generatedLeft = new();
    private readonly List<Node> _generatedRight = new();

    private readonly Dictionary<int, bool> _leftActive = new();

    public override void _Ready()
    {
        _leftList = GetNodeOrNull<Node3D>(LeftListPath);
        _leftTemplate = GetNodeOrNull<Node3D>(LeftItemTemplatePath);

        _rightComponent = GetNodeOrNull<Node3D>(RightComponentPath);
        _rightContent = GetNodeOrNull<Node3D>(RightContentPath);

        _rightTitleText = GetNodeOrNull<MeshInstance3D>(RightTitleTextPath);
        _bottomPathText = GetNodeOrNull<MeshInstance3D>(BottomPathTextPath);

        _closeButton = GetNodeOrNull<PhysicalHitBoxButton3D>(CloseButtonPath);

        if (_closeButton != null)
        {
            _closeButton.Prompt = "Close";
            _closeButton.OnPressed = _ => Close();
        }
    }

    public void SetHeader(string title)
    {
        if (_rightTitleText != null)
            SetMeshTextUnique(_rightTitleText, title);
    }

    public void SetBottomPath(string path)
    {
        if (_bottomPathText != null)
            SetMeshTextUnique(_bottomPathText, path);
    }

    public void SetLeftItems(IReadOnlyList<(string label, bool active)> items)
    {
        if (_leftList == null || _leftTemplate == null)
        {
            GD.PrintErr("TemplateUiPhysicalGenerator3D: Left list nodes not found. Check LeftListPath/LeftItemTemplatePath.");
            return;
        }

        if (ClearGeneratedOnSet)
            ClearGenerated(_generatedLeft);

        _leftActive.Clear();

        if (items.Count == 0)
        {
            _leftTemplate.Visible = false;
            return;
        }

        _leftTemplate.Visible = true;

        var basePos = _leftTemplate.Position;

        // Keep the template as item 0 if possible (so the scene looks usable in-editor).
        // We'll still overwrite its content as the first item when items.Count > 0.
        for (int i = 0; i < items.Count; i++)
        {
            var (label, active) = items[i];
            _leftActive[i] = active;

            Node3D itemNode;
            if (i == 0)
            {
                itemNode = _leftTemplate;
            }
            else
            {
                itemNode = (Node3D)_leftTemplate.Duplicate();
                itemNode.Name = $"GeneratedLeftItem_{i}";
                _leftList.AddChild(itemNode);
                _generatedLeft.Add(itemNode);
            }

            itemNode.Position = basePos + new Vector3(0, -LeftItemSpacing * i, 0);

            var title = itemNode.GetNodeOrNull<MeshInstance3D>("TitleText");
            if (title != null)
                SetMeshTextUnique(title, label);

            var toggle = itemNode.GetNodeOrNull<PhysicalHitBoxButton3D>("ActiveTogglebutton");
            if (toggle != null)
            {
                int idx = i;
                toggle.Prompt = "Toggle";
                toggle.OnPressed = _ => ToggleLeftActive(idx);

                // Optional: visually hint active state by scaling depth slightly.
                // Keep minimal and non-stylized: just a subtle depth tweak.
                toggle.Size = new Vector3(toggle.Size.X, toggle.Size.Y, active ? 0.016f : 0.0098f);
                toggle.RefreshHitArea();
            }
        }
    }

    public void SetRightRows(IReadOnlyList<(string key, string value)> rows)
    {
        if (_rightContent == null)
        {
            GD.PrintErr("TemplateUiPhysicalGenerator3D: Right content node not found. Check RightContentPath.");
            return;
        }

        if (ClearGeneratedOnSet)
            ClearGenerated(_generatedRight);

        HideHardcodedRightRows(_rightContent);

        // Try to find an existing row template (Content/Item) from the scene.
        var rowTemplate = _rightContent.GetNodeOrNull<MeshInstance3D>("Item");
        var rowTemplateBox = rowTemplate?.GetNodeOrNull<CsgBox3D>("TextContent");

        Vector3 basePos = rowTemplate?.Position ?? Vector3.Zero;

        for (int i = 0; i < rows.Count; i++)
        {
            var (key, value) = rows[i];

            MeshInstance3D keyNode;
            CsgBox3D valueBox;

            if (i == 0 && rowTemplate != null && rowTemplateBox != null)
            {
                keyNode = rowTemplate;
                valueBox = rowTemplateBox;
                keyNode.Visible = true;
                valueBox.Visible = true;
            }
            else
            {
                // If the template exists, duplicate it so we preserve the physical look.
                if (rowTemplate != null)
                {
                    keyNode = (MeshInstance3D)rowTemplate.Duplicate();
                    keyNode.Name = $"GeneratedRightRow_{i}_Key";
                    _rightContent.AddChild(keyNode);
                    _generatedRight.Add(keyNode);

                    valueBox = keyNode.GetNodeOrNull<CsgBox3D>("TextContent") ?? new CsgBox3D { Name = "TextContent" };
                    if (valueBox.GetParent() == null)
                        keyNode.AddChild(valueBox);
                }
                else
                {
                    // Fallback: create a basic key mesh.
                    keyNode = new MeshInstance3D { Name = $"GeneratedRightRow_{i}_Key" };
                    keyNode.Mesh = new TextMesh { Text = key };
                    _rightContent.AddChild(keyNode);
                    _generatedRight.Add(keyNode);

                    valueBox = new CsgBox3D { Name = "TextContent" };
                    keyNode.AddChild(valueBox);
                }
            }

            keyNode.Position = basePos + new Vector3(0, -RightRowSpacing * i, 0);
            SetMeshTextUnique(keyNode, key);

            EnsureValueText(valueBox, value, keyNode);
        }

        // If caller sets 0 rows, hide the template row.
        if (rows.Count == 0 && rowTemplate != null)
            rowTemplate.Visible = false;
    }

    public void Close()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private void ToggleLeftActive(int index)
    {
        var active = _leftActive.TryGetValue(index, out var current) ? !current : true;
        _leftActive[index] = active;
        EmitSignal(SignalName.LeftItemToggled, index, active);

        // If we can find the toggle node again, update the depth hint.
        if (_leftList != null)
        {
            var itemName = index == 0 ? _leftTemplate?.Name.ToString() : $"GeneratedLeftItem_{index}";
            if (!string.IsNullOrEmpty(itemName))
            {
                var item = _leftList.GetNodeOrNull<Node3D>(itemName);
                var toggle = item?.GetNodeOrNull<PhysicalHitBoxButton3D>("ActiveTogglebutton");
                if (toggle != null)
                {
                    toggle.Size = new Vector3(toggle.Size.X, toggle.Size.Y, active ? 0.016f : 0.0098f);
                    toggle.RefreshHitArea();
                }
            }
        }
    }

    private static void HideHardcodedRightRows(Node rightContent)
    {
        // template_ui.tscn currently has Item2/Item3 as baked examples.
        // Keep only "Item" as the template row.
        foreach (var childObj in rightContent.GetChildren())
        {
            if (childObj is MeshInstance3D mi)
            {
                var name = mi.Name.ToString();
                if (name != "Item" && name.StartsWith("Item", StringComparison.Ordinal))
                    mi.Visible = false;
            }
        }
    }

    private static void ClearGenerated(List<Node> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (IsInstanceValid(list[i]))
                list[i].QueueFree();
        }
        list.Clear();
    }

    private static void EnsureValueText(CsgBox3D valueBox, string value, MeshInstance3D referenceMesh)
    {
        // Create (or reuse) a MeshInstance3D called ValueText under the value box.
        var valueText = valueBox.GetNodeOrNull<MeshInstance3D>("ValueText");
        if (valueText == null)
        {
            valueText = new MeshInstance3D { Name = "ValueText" };
            valueBox.AddChild(valueText);

            // Try to borrow style from the reference key mesh.
            if (referenceMesh.Mesh is TextMesh refText)
            {
                var dup = (TextMesh)refText.Duplicate();
                dup.Text = value;
                valueText.Mesh = dup;
            }
            else
            {
                valueText.Mesh = new TextMesh { Text = value };
            }

            // Place slightly above the box so it is readable.
            valueText.Position = new Vector3(0, 0.06f, 0);
            valueText.Scale = new Vector3(1, 1, 1);
        }
        else
        {
            SetMeshTextUnique(valueText, value);
        }
    }

    private static void SetMeshTextUnique(MeshInstance3D meshInstance, string text)
    {
        if (meshInstance.Mesh is TextMesh tm)
        {
            // If multiple instances share the same TextMesh resource, mutate a duplicate.
            var unique = (TextMesh)tm.Duplicate();
            unique.Text = text;
            meshInstance.Mesh = unique;
        }
        else
        {
            // Fallback if the mesh isn't a TextMesh.
            var newTm = new TextMesh { Text = text };
            meshInstance.Mesh = newTm;
        }
    }
}
