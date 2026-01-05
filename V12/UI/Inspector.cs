using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using V12.Components;
using V12.Interfaces;

namespace V12.UI
{
    public partial class Inspector : GodotObject
    {
        public Control UIRoot { get; private set; }
        private Tree _tree;
        private VBoxContainer _componentVBox;
        private OptionButton _componentSelector;
        private Button _addComponentButton;
        private IWorldElement? _selectedElement;
        private List<Type> _availableComponents = new();
        private Dictionary<TreeItem, IWorldElement> _itemToElement = new();

        public Inspector(IWorldElement root)
        {
            UIRoot = new PanelContainer { Name = "InspectorRoot" };
            // add a simple border/background style to make the UI stand out
            var style = new StyleBoxFlat();
            style.BgColor = Colors.White;
            style.BorderWidthBottom = 2;
            style.BorderWidthLeft = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthTop = 2;

            style.BorderColor = Colors.Black;
            UIRoot.AddThemeStyleboxOverride("panel", style);
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

            // Right side: toolbar + scroll area
            var right = new VBoxContainer();
            right.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            right.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

            var toolbar = new HBoxContainer();
            toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            var toolbarLabel = new Label { Text = "Add component:", SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            _componentSelector = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            _addComponentButton = new Button { Text = "Add", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
            _addComponentButton.Pressed += OnAddComponentPressed;

            toolbar.AddChild(toolbarLabel);
            toolbar.AddChild(_componentSelector);
            toolbar.AddChild(_addComponentButton);
            right.AddChild(toolbar);

            // Scrollable VBox for component UIs
            var scroll = new ScrollContainer();
            scroll.SizeFlagsHorizontal = Control.SizeFlags.Expand;
            scroll.SizeFlagsVertical = Control.SizeFlags.Expand;
            scroll.ClipContents = false; // this set to true hides the contents apparently?

            _componentVBox = new VBoxContainer();
            _componentVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _componentVBox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            _componentVBox.AddThemeConstantOverride("separation", 8);
            scroll.AddChild(_componentVBox);
            right.AddChild(scroll);
            split.AddChild(right);

            UIRoot.AddChild(split);
            GenerateUI(root);
            BuildAvailableComponents();
            PopulateComponentSelector();
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
                GD.Print($"[Inspector] added child: {child.Name}");
                var item = tree.CreateItem(parentItem);
                item.SetCustomColor(0, Colors.White);
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
                _selectedElement = element;
                GD.Print($"Selected element: {element.Name}, components: {element.Components.Count}");
                RefreshComponentUI(element);
            }
        }

        private void RefreshComponentUI(IWorldElement element)
        {
            foreach (var c in _componentVBox.GetChildren())
                c.QueueFree();

            foreach (var comp in element.Components)
            {
                GD.Print($"Component: {comp.GetType().Name}");
                var card = new PanelContainer();
                var style = new StyleBoxFlat
                {
                    BgColor = new Color(0.16f, 0.16f, 0.16f)
                };
                style.BorderWidthBottom = style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = 1;
                style.BorderColor = new Color(0.35f, 0.35f, 0.35f);
                card.AddThemeStyleboxOverride("panel", style);

                var cardVBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                cardVBox.AddThemeConstantOverride("separation", 4);
                var header = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                var title = new Label
                {
                    Text = comp.GetType().Name,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                };
                var removeButton = new Button { Text = "Remove", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
                removeButton.Pressed += () =>
                {
                    element.RemoveComponent(comp);
                    comp.OnDetach();
                    RefreshComponentUI(element);
                };
                header.AddChild(title);
                header.AddChild(removeButton);
                cardVBox.AddChild(header);

                if (!string.IsNullOrWhiteSpace(comp.Description))
                {
                    var desc = new Label
                    {
                        Text = comp.Description,
                        AutowrapMode = TextServer.AutowrapMode.Word,
                        Modulate = new Color(0.8f, 0.8f, 0.8f)
                    };
                    cardVBox.AddChild(desc);
                }

                var ui = comp.GenerateUI();
                if (ui is Control control)
                {
                    control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    cardVBox.AddChild(control);
                }

                card.AddChild(cardVBox);
                _componentVBox.AddChild(card);
            }
        }

        private void OnAddComponentPressed()
        {
            if (_selectedElement == null || _availableComponents.Count == 0)
                return;

            var selectedIdx = _componentSelector.GetSelectedId();
            if (selectedIdx < 0 || selectedIdx >= _availableComponents.Count)
                return;

            var type = _availableComponents[selectedIdx];
            try
            {
                if (Activator.CreateInstance(type) is IComponent comp)
                {
                    comp.OnAwake();
                    _selectedElement.AddComponent(comp);
                    RefreshComponentUI(_selectedElement);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Inspector] Failed to add component '{type.Name}': {ex.Message}");
            }
        }

        private void BuildAvailableComponents()
        {
            _availableComponents = typeof(Component).Assembly
                .GetTypes()
                .Where(t => typeof(IComponent).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => t != typeof(Components.Inspector))
                .OrderBy(t => t.Name)
                .ToList();
        }

        private void PopulateComponentSelector()
        {
            _componentSelector.Clear();
            for (int i = 0; i < _availableComponents.Count; i++)
            {
                _componentSelector.AddItem(_availableComponents[i].Name, i);
            }
            if (_availableComponents.Count > 0)
            {
                _componentSelector.Select(0);
            }
        }

        public static implicit operator Node3D(Inspector v)
        {
            var node = new Node3D();
            node.AddChild(v.UIRoot);
            return node;
        }
    }
}