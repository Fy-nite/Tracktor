#nullable enable
using Godot;
using System;

namespace Traktion.CSharp.Construction;

public partial class InspectorPanel2D : Control
{
    [Signal] public delegate void ClosedEventHandler();

    [Export] public NodePath InspectRootPath { get; set; } = new();

    private Tree? _tree;
    private Button? _close;

    public override void _Ready()
    {
        _tree = GetNodeOrNull<Tree>("Panel/VBox/Tree");
        _close = GetNodeOrNull<Button>("Panel/VBox/TopRow/Close");

        if (_close != null)
            _close.Pressed += Close;

        Rebuild();
    }

    public void Rebuild()
    {
        if (_tree == null)
            return;

        _tree.Clear();
        _tree.HideRoot = true;

        var root = ResolveInspectRoot();
        if (root == null)
            return;

        var treeRoot = _tree.CreateItem();
        AddNodeRecursive(treeRoot, root);
    }

    public void Close()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private Node? ResolveInspectRoot()
    {
        if (!InspectRootPath.IsEmpty)
            return GetNodeOrNull(InspectRootPath);

        return GetTree().CurrentScene;
    }

    private void AddNodeRecursive(TreeItem parent, Node node)
    {
        var item = _tree!.CreateItem(parent);

        var typeName = node.GetType().Name;
        item.SetText(0, $"{node.Name} ({typeName})");

        // Store a stable reference string (scene-unique path) for potential future use.
        item.SetMetadata(0, node.GetPath().ToString());

        foreach (var childObj in node.GetChildren())
        {
            if (childObj is Node child)
                AddNodeRecursive(item, child);
        }
    }
}
