using Godot;
using V12.Interfaces;

public class WorldElementSceneSyncer
{
    private IWorldElement _rootElement;
    private Node3D _sceneRoot;
    private Dictionary<IWorldElement, Node> _elementToNode = new();
    private Dictionary<IComponent, Node> _componentToNode = new();

    public bool DebugMode { get; set; } = true;

    public WorldElementSceneSyncer(IWorldElement rootElement, Node3D sceneRoot)
    {
        _rootElement = rootElement;
        _sceneRoot = sceneRoot;
        DebugPrint($"[Init] Syncing tree for root element: {rootElement?.Name ?? "<null>"}");
        SyncTree(_rootElement, _sceneRoot);

        // Subscribe to events for dynamic updates
        _rootElement.OnElementAdded += OnElementAdded;
        _rootElement.OnElementRemoved += OnElementRemoved;
    }

    private void DebugPrint(string msg)
    {
        if (DebugMode)
            GD.Print($"[WorldElementSceneSyncer] {msg}");
    }

    private void SyncTree(IWorldElement element, Node parentNode)
    {
        Node node = CreateNodeForElement(element);
        DebugPrint($"Syncing Element: {element.Name} as Node: {node.Name} (Parent: {parentNode.Name})");
        parentNode.AddChild(node);
        _elementToNode[element] = node;

        element.OnElementChanged += OnElementChanged;
        element.OnComponentAdded += OnComponentAdded;
        element.OnComponentRemoved += OnComponentRemoved;

        DebugPrint($"Element '{element.Name}' has {element.Components.Count} components.");
        foreach (var comp in element.Components)
        {
            DebugPrint($" - Component: type={comp?.GetType().FullName ?? "<null>"} name='{comp?.Name ?? ""}'");
            AddComponentNode(comp, node);
        }

        foreach (var child in element.ChildElements)
        {
            SyncTree(child, node);
        }
    }

    private Node CreateNodeForElement(IWorldElement element)
    {
        Node3D node = new Node3D();
        node.Name = element.Name ?? "WorldElement";
        DebugPrint($"Created Node for Element: {element.Name} at Position: {node.Position}");
        return node;
    }

    private void OnElementAdded(IWorldElement element)
    {
        if (element.ParentElement != null && _elementToNode.TryGetValue(element.ParentElement, out var parentNode))
        {
            DebugPrint($"Element Added: {element.Name} (Parent: {element.ParentElement.Name})");
            // Create node for this element and recursively sync its subtree
            Node node = CreateNodeForElement(element);
            parentNode.AddChild(node);
            _elementToNode[element] = node;
            element.OnElementChanged += OnElementChanged;
            element.OnComponentAdded += OnComponentAdded;
            element.OnComponentRemoved += OnComponentRemoved;

            // Attach components for this element
            foreach (var comp in element.Components)
            {
                // Ensure component ParentElement is set
                comp.ParentElement = element;
                AddComponentNode(comp, node);
            }

            // Recursively sync any existing child elements
            foreach (var child in element.ChildElements)
            {
                SyncTree(child, node);
            }
        }
        else
        {
            DebugPrint($"[Warning] Tried to add element {element.Name} but parent node not found.");
        }
    }

    private void OnElementRemoved(IWorldElement element)
    {
        if (_elementToNode.TryGetValue(element, out var node))
        {
            DebugPrint($"Element Removed: {element.Name}");
            element.OnElementChanged -= OnElementChanged;
            element.OnComponentAdded -= OnComponentAdded;
            element.OnComponentRemoved -= OnComponentRemoved;

            node.QueueFree();
            _elementToNode.Remove(element);
        }
        else
        {
            DebugPrint($"[Warning] Tried to remove element {element.Name} but node not found.");
        }
    }

    private void OnElementChanged(IWorldElement element)
    {
        if (_elementToNode.TryGetValue(element, out var node) && node is Node3D node3D)
        {
            DebugPrint($"Element Changed: {element.Name} | Pos: {element.Position} Rot: {element.Rotation} Scale: {element.Scale}");
            node3D.Position = new Vector3((float)element.Position.x, (float)element.Position.y, (float)element.Position.z);
            node3D.Rotation = new Vector3((float)element.Rotation.x, (float)element.Rotation.y, (float)element.Rotation.z);
            node3D.Scale = new Vector3((float)element.Scale.x, (float)element.Scale.y, (float)element.Scale.z);
            node3D.Name = element.Name ?? "WorldElement";
        }
        else
        {
            DebugPrint($"[Warning] Element changed but node not found for {element.Name}");
        }
    }

    private void AddComponentNode(IComponent comp, Node parent)
    {
        // attach component logic directly to parent node
        DebugPrint($"Running RunComponentLogic for component type: {comp?.GetType().FullName ?? "<null>"} name: '{comp?.Name ?? ""}'");
        var compNode = comp.RunComponentLogic();
        DebugPrint($"RunComponentLogic returned node type: {compNode?.GetType().FullName ?? "<null>"} name: {compNode?.Name ?? "<null>"}");
        DebugPrint($"Adding Component: {comp.Name} as Node: {compNode?.Name ?? "<null>"} to Parent: {parent.Name}");
        if (compNode == null)
        {
            DebugPrint($"[Info] Component {comp.Name} did not return a node. Creating default Node3D.");
            compNode = new Node3D { Name = comp.Name };
        }
        else if (compNode.GetParent() != null)
        {
            DebugPrint($"[Warning] Component node {compNode.Name} already has a parent. Removing from previous parent.");
            compNode.GetParent().RemoveChild(compNode);
        }
        parent.AddChild(compNode);
        _componentToNode[comp] = compNode;

        // Notify component its scene node has been assigned so it can cache references
        try
        {
            comp.OnSceneNodeAssigned(compNode);
        }
        catch (Exception ex)
        {
            DebugPrint($"[Error] Exception in OnSceneNodeAssigned for {comp.Name}: {ex.Message}");
        }
    }

    private void OnComponentAdded(IComponent comp)
    {
        if (_elementToNode.TryGetValue(comp.ParentElement, out var parent))
        {
            DebugPrint($"Component Added: {comp.Name} to Parent Element: {comp.ParentElement?.Name}");
            AddComponentNode(comp, parent);
        }
        else
        {
            DebugPrint($"[Warning] Tried to add component {comp.Name} but parent element node not found.");
        }
    }

    private void OnComponentRemoved(IComponent comp)
    {
        if (_componentToNode.TryGetValue(comp, out var node))
        {
            DebugPrint($"Component Removed: {comp.Name}");
            node.QueueFree();
            _componentToNode.Remove(comp);

            try
            {
                comp.OnSceneNodeRemoved();
            }
            catch (Exception ex)
            {
                DebugPrint($"[Error] Exception in OnSceneNodeRemoved for {comp.Name}: {ex.Message}");
            }
        }
        else
        {
            DebugPrint($"[Warning] Tried to remove component {comp.Name} but node not found.");
        }
    }

    // Public method to run per-frame ticks on components. Caller (e.g. main engine)
    // should call this each frame with delta time to allow components to update.
    public void Process(float delta)
    {
        foreach (var kv in _componentToNode)
        {
            var comp = kv.Key;
            try
            {
                comp.OnTick(delta);
            }
            catch (Exception ex)
            {
                DebugPrint($"[Error] Exception in OnTick for {comp.Name}: {ex.Message}");
            }
        }
    }

    // Public lookup helpers so external code (components, tools, etc.) can find
    // the Godot Node associated with a world element or component.
    public Node? GetNodeForElement(IWorldElement element)
    {
        if (element == null) return null;
        _elementToNode.TryGetValue(element, out var node);
        return node;
    }

    public Node? GetNodeForComponent(IComponent component)
    {
        if (component == null) return null;
        _componentToNode.TryGetValue(component, out var node);
        return node;
    }
}