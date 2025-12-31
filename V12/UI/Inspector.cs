using Godot;
using V12.Interfaces;
using System.Collections.Generic;

namespace V12.UI
{
    public class Inspector : GodotObject
    {
        public Control UIRoot { get; private set; }
        private Tree _tree;
        private VBoxContainer _componentVBox;
        private Dictionary<TreeItem, IWorldElement> _itemToElement = new();

        public Inspector(IWorldElement root)
        {
            UIRoot = new PanelContainer { Name = "InspectorRoot" };
            UIRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            UIRoot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            UIRoot.Size = new Vector2(400, 600);

            var split = new HSplitContainer();
            split.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            split.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            split.Size = new Vector2(400, 600);

            // Left: Scene tree
            _tree = new Tree();
            _tree.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _tree.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _tree.AnchorRight = 1;
            _tree.AnchorBottom = 1;
            _tree.ItemSelected += OnTreeItemSelected;

            split.AddChild(_tree);

            // Right: Scrollable VBox for component UIs
            var scroll = new ScrollContainer();
            scroll.SizeFlagsHorizontal = Control.SizeFlags.Expand;
            scroll.SizeFlagsVertical = Control.SizeFlags.Expand;
            scroll.ClipContents = false; // this set to true hides the contents apparently?

            _componentVBox = new VBoxContainer();
            _componentVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _componentVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scroll.AddChild(_componentVBox);
            split.AddChild(scroll);

            UIRoot.AddChild(split);
            GenerateUI(root);
        }

      

        public void GenerateUI(IWorldElement rootElement)
        {
            _tree.Clear();
            _itemToElement.Clear();
            var root = _tree.CreateItem();
            root.SetText(0, rootElement.Name ?? "Root");
            _itemToElement[root] = rootElement;
            AddElementToTree(rootElement, root, _tree);
           
        }

        private void AddElementToTree(IWorldElement element, TreeItem parentItem, Tree tree)
        {
            foreach (var child in element.ChildElements)
            {
                GD.Print($"added child: {child.Name}");
                var item = tree.CreateItem(parentItem);
                item.SetText(0, child.Name ?? "Element");
                _itemToElement[item] = child; // Store mapping
                AddElementToTree(child, item, tree);
            }
        }

        private void OnTreeItemSelected()
        {
            // Clear previous UI
            foreach (var c in _componentVBox.GetChildren())
                c.QueueFree();

            var selected = _tree.GetSelected();
            if (selected == null) return;
            if (_itemToElement.TryGetValue(selected, out var element) && element != null)
            {
                GD.Print($"Selected element: {element.Name}, components: {element.Components.Count}");
                foreach (var comp in element.Components)
                {
                    GD.Print($"Component: {comp.Name}");
                    var ui = comp.GenerateUI();
                    if (ui is Control control)
                        _componentVBox.AddChild(control);
                }
            }
        }
    }
}