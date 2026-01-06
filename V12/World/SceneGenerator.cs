using System.Diagnostics;
using Godot;
using V12.Components;
using V12.Interfaces;
using V12.Types;

public class SceneGenerator
{
    public Node3D sceneRoot { get; private set; }
    public IWorldElement rootElement { get; private set; }
    const bool DEBUGMODE = true;
    public SceneGenerator(IWorldElement rootElement, Node3D sceneRoot)
    {
        this.sceneRoot = sceneRoot;
        this.rootElement = rootElement;
        DebugPrint($"[Constructor] New SceneGenerator created");
    }
    public void DebugPrint(string msg)
    {
        if (DEBUGMODE)
            GD.Print($"[SceneGenerator] {msg}");
    }
    public void RegenerateTree() { RegenerateTree(rootElement, sceneRoot); }
    public void RegenerateTree(IWorldElement rootElement) { RegenerateTree(rootElement, rootElement.sceneElement ?? sceneRoot); }
    public void RegenerateTree(Node3D sceneRoot) { RegenerateTree(rootElement, sceneRoot); }
    public void RegenerateTree(IWorldElement rootElement, Node3D sceneRoot)
    {
        foreach (var child in sceneRoot.GetChildren())
        {
            child.QueueFree();
        }
        rootElement.sceneElement = sceneRoot;
        List<IWorldElement> elements = FlattenTree(rootElement);
        AwakeComponents(rootElement);
        foreach (var elem in elements)
        {
            DebugPrint($"[RegenerateTree] Generating Element {elem.Name}");
            foreach (var child in elem.ChildElements)
                DebugPrint($"[RegenerateTree]   - Has Child {child.Name}");
            foreach (var comp in elem.Components)
                DebugPrint($"[RegenerateTree]   - Has Component {(comp.Name != null && comp.Name.Length != 0 ? comp.Name : comp.GetType().Name)}");
            GenerateElement(elem);
        }
    }
    public void AwakeComponents(IWorldElement root)
    {
        foreach (var elem in root.ChildElements)
        {
            elem.ParentElement = root;
            AwakeComponents(elem);
        }
        foreach (var comp in root.Components)
        {
            comp.attachedElement = root;
            comp.OnAwake();
        }
    }
    public List<IWorldElement> FlattenTree(IWorldElement root)
    {
        List<IWorldElement> return_val = new List<IWorldElement> { root };
        foreach (IWorldElement child in root.ChildElements)
        {
            child.ParentElement = root;
            return_val.AddRange(FlattenTree(child));
        }
        return return_val;
    }
    public void GenerateElement(IWorldElement element)
    {
        Node3D elemNode = new Node3D();
        Node3D parent = sceneRoot;
        if (element.ParentElement != null)
            parent = element.ParentElement.sceneElement;
        parent.AddChild(elemNode);
        element.sceneElement = elemNode;

        foreach (var component in element.Components)
        {
            component.OnAttach();
            DebugPrint($"[GenerateElement] Attached Component {component.Name}");
            if (component is NodeComponent nc)
                DebugPrint($"    Node {nc.sceneElement?.Name}");
        }
        elemNode.Name = element.Name == null || element.Name.Length == 0 ? "Empty Name" : element.Name;
        elemNode.Position = element.Position.ToVector3();
        elemNode.Rotation = element.Rotation.ToVector3();
        elemNode.Scale = element.Scale.ToVector3();

        element.OnElementAdded += (e) => { Node3D root = new(); e.ParentElement.sceneElement?.AddChild(root); RegenerateTree(e, root); };
        element.OnElementChanged += (e) => { RegenerateTree(e); };
        element.OnRootChanged += (e) => { RegenerateTree(e); };
        element.OnElementRemoved += (e) => { element.sceneElement.QueueFree(); };
        element.OnComponentAdded += (e) => { if (e.attachedElement != null) RegenerateElement(e.attachedElement); };
        element.OnComponentRemoved += (e) => { if (e.attachedElement != null) RegenerateElement(e.attachedElement); };
    }
    public void RegenerateElement(IWorldElement element)
    {
        var oldElem = element.sceneElement;
        //element.sceneElement = null;
        GenerateElement(element);
        oldElem?.QueueFree();
    }
}
