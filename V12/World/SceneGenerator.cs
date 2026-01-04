using System.Diagnostics;
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
	public void RegenerateTree() {RegenerateTree(rootElement, sceneRoot);}
	public void RegenerateTree(IWorldElement rootElement) {RegenerateTree(rootElement, rootElement.sceneElement);}
	public void RegenerateTree(Node3D sceneRoot) {RegenerateTree(rootElement, sceneRoot);}
	public void RegenerateTree(IWorldElement rootElement, Node3D sceneRoot)
	{
		foreach (var child in sceneRoot.GetChildren())
		{
			child.QueueFree();
		}
		// rootElement.sceneElement == sceneRoot
		List<IWorldElement> elements = FlattenTree(rootElement);
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
	private List<IWorldElement> FlattenTree(IWorldElement root)
	{
		List<IWorldElement> return_val = new List<IWorldElement> {root};
		foreach (IWorldElement child in root.ChildElements)
		{
			child.ParentElement = root;
			return_val.AddRange(FlattenTree(child));
		}
		return return_val;
	}
	private void GenerateElement(IWorldElement element)
	{
		Node3D elemNode = new Node3D();
		Node3D parent = sceneRoot;
		if (element.ParentElement != null)
			parent = element.ParentElement.sceneElement;
		elemNode.Name = element.Name;
		elemNode.Position = element.Position.ToVector3();
		elemNode.Rotation = element.Rotation.ToVector3();
		elemNode.Scale = element.Scale.ToVector3();
		parent.AddChild(elemNode);

		element.sceneElement = elemNode;
		
		foreach (var component in element.Components)
		{
			component.attachedElement = element;
			component.OnAwake();
			component.OnAttach();
		}

		element.OnElementAdded += (e) => {GenerateElement(e); RegenerateTree(e);};
		element.OnElementChanged += (e) => {RegenerateElement(e); RegenerateTree(e);};
		element.OnRootChanged += (e) => {RegenerateElement(e); RegenerateTree(e);};
		element.OnElementRemoved += (e) => {element.sceneElement.QueueFree();};
		element.OnComponentAdded += (e) => {if (e.attachedElement != null) RegenerateElement(e.attachedElement);};
		element.OnComponentRemoved += (e) => {if (e.attachedElement != null) RegenerateElement(e.attachedElement);};
	}
	private void RegenerateElement(IWorldElement element)
	{
		element.sceneElement.QueueFree();
		GenerateElement(element);
	}
}
