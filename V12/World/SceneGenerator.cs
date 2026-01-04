using Godot;
using V12.Interfaces;
using V12.Types;

public class SceneGenerator
{
    private Node3D sceneRoot;
    private IWorldElement rootElement;
    const bool DEBUGMODE = true;
    public SceneGenerator(IWorldElement rootElement, Node3D sceneRoot)
    {
        this.sceneRoot = sceneRoot;
        this.rootElement = rootElement;
        DebugPrint($"[Constructor] New SceneGenerator created");
    }
    private void DebugPrint(string msg)
    {
        if (DEBUGMODE)
            GD.Print($"[SceneGenerator] {msg}");
    }
    public void RegenerateTree()
    {
        foreach (var child in sceneRoot.GetChildren())
        {
            child.QueueFree();
        }
        // rootElement.sceneElement == sceneRoot
        List<IWorldElement> elements = FlattenTree(rootElement);
    }
    private List<IWorldElement> FlattenTree(IWorldElement root)
    {
        List<IWorldElement> return_val = new();
        return_val.Add(root);
        foreach (IWorldElement child in root.ChildElements)
        {
            FlattenTree(child);
        }
        return return_val;
    }
    private void GenerateElement(IWorldElement element, Node3D parent)
    {
        Node3D elemNode = new Node3D();
        elemNode.Name = element.Name;
        elemNode.Position = element.Position.ToVector3();
        elemNode.Rotation = element.Rotation.ToVector3();
        elemNode.Scale = element.Scale.ToVector3();
        parent.AddChild(elemNode);
        foreach (var component in element.Components)
        {
            component.OnAttach();
        }
        element.sceneElement = elemNode;
    }
}