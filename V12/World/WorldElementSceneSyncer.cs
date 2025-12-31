using Godot;
using System.Collections.Generic;
using V12.Interfaces;
using V12.Components.VR;
using V12.Components;

public class WorldElementSceneSyncer
{
    private IWorldElement _rootElement;
    private Node3D _sceneRoot;
    private Dictionary<IWorldElement, Node> _elementToNode = new();
    private Dictionary<IComponent, Node> _componentToNode = new();

    public WorldElementSceneSyncer(IWorldElement rootElement, Node3D sceneRoot)
    {
        _rootElement = rootElement;
        _sceneRoot = sceneRoot;
        SyncTree(_rootElement, _sceneRoot);

        // Subscribe to events for dynamic updates
        _rootElement.OnElementAdded += OnElementAdded;
        _rootElement.OnElementRemoved += OnElementRemoved;
    }

    private void SyncTree(IWorldElement element, Node parentNode)
    {
        Node node = CreateNodeForElement(element);
        parentNode.AddChild(node);
        _elementToNode[element] = node;

        element.OnElementChanged += OnElementChanged;
        element.OnComponentAdded += OnComponentAdded;
        element.OnComponentRemoved += OnComponentRemoved;

        foreach (var comp in element.Components)
        {
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
        return node;
    }

    private void OnElementAdded(IWorldElement element)
    {
        if (element.ParentElement != null && _elementToNode.TryGetValue(element.ParentElement, out var parentNode))
        {
            Node node = CreateNodeForElement(element);
            parentNode.AddChild(node);
            _elementToNode[element] = node;
            element.OnElementChanged += OnElementChanged;
            element.OnComponentAdded += OnComponentAdded;
            element.OnComponentRemoved += OnComponentRemoved;

            foreach (var comp in element.Components)
            {
                AddComponentNode(comp, node);
            }
        }
    }

    private void OnElementRemoved(IWorldElement element)
    {
        if (_elementToNode.TryGetValue(element, out var node))
        {
            element.OnElementChanged -= OnElementChanged;
            element.OnComponentAdded -= OnComponentAdded;
            element.OnComponentRemoved -= OnComponentRemoved;

            node.QueueFree();
            _elementToNode.Remove(element);
        }
    }

    private void OnElementChanged(IWorldElement element)
    {
        if (_elementToNode.TryGetValue(element, out var node) && node is Node3D node3D)
        {
            node3D.Position = new Vector3((float)element.Position.x, (float)element.Position.y, (float)element.Position.z);
            node3D.Rotation = new Vector3((float)element.Rotation.x, (float)element.Rotation.y, (float)element.Rotation.z);
            node3D.Scale = new Vector3((float)element.Scale.x, (float)element.Scale.y, (float)element.Scale.z);
            node3D.Name = element.Name ?? "WorldElement";
        }
    }

    private void AddComponentNode(IComponent comp, Node parent)
    {
        // attach component logic directly to parent node

        //switch (comp)
        //{
        //    case V12.Components.BoxMesh boxMesh:
        //        {
        //            GD.Print($"box mesh added {boxMesh}");
        //            var csgBox = new CsgBox3D
        //            {
        //                Name = boxMesh.Name,
        //                Size = boxMesh.Scale, // Godot expects full size, not half-extents
        //                Operation = CsgShape3D.OperationEnum.Union
        //            };
        //            parent.AddChild(csgBox);
        //            _componentToNode[comp] = csgBox;
        //            break;
        //        }
        //    case IMesh meshComp:
        //    {
        //        var generated = meshComp.GenerateGodotMesh();
        //        if (generated == null)
        //        {
        //            GD.PushWarning($"IMesh component '{comp.Name}' generated a null mesh (type: {meshComp.GetType().Name}).");
        //            break;
        //        }

        //        var meshInstance = new MeshInstance3D
        //        {
        //            Name = comp.Name,
        //            Mesh = generated
        //        };
        //        parent.AddChild(meshInstance);
        //        _componentToNode[comp] = meshInstance;
        //        break;
        //    }

        //    case VROriginComponent:
        //    {
        //        Node origin;
        //        try
        //        {
        //            origin = new XROrigin3D { Name = comp.Name };
        //        }
        //        catch
        //        {
        //            origin = new Node3D { Name = comp.Name };
        //        }

        //        parent.AddChild(origin);
        //        _componentToNode[comp] = origin;
        //        break;
        //    }
        //    case VRCameraComponent camComp:
        //    {
        //        Node camera;
        //        try
        //        {
        //            var xrCam = new XRCamera3D { Name = comp.Name };
        //            xrCam.Position = new Vector3(0, camComp.HeightOffset, 0);
        //            camera = xrCam;
        //        }
        //        catch
        //        {
        //            var cam = new Camera3D { Name = comp.Name };
        //            cam.Position = new Vector3(0, camComp.HeightOffset, 0);
        //            camera = cam;
        //        }

        //        parent.AddChild(camera);
        //        _componentToNode[comp] = camera;
        //        break;
        //    }
        //    case VRControllerComponent ctrl:
        //    {
        //            //Node controller;
        //            //try
        //            //{
        //            //    controller = new XRController3D
        //            //    {
        //            //        Name = comp.Name,
        //            //        Tracker = ctrl.Hand == true ? new StringName ("left_hand") : new StringName("right_hand")
        //            //    };
        //            //}
        //            //catch
        //            //{
        //            //    controller = new Node3D { Name = comp.Name };
        //            //}
        //            Node controller = ctrl.RunComponentLogic();
        //        parent.AddChild(controller);
        //        _componentToNode[comp] = controller;
        //        break;
        //    }
        //    case VRRigComponent vrRig:
        //    {
        //        // Convenience: if someone still adds VRRigComponent, build a default rig subtree.
        //        // Prefer writing rigs using VROriginComponent/VRCameraComponent/VRControllerComponent.
        //        var rigRoot = new Node3D { Name = comp.Name };
        //        parent.AddChild(rigRoot);

        //        // Build under rigRoot
        //        var originEl = new VROriginComponent();
        //        AddComponentNode(originEl, rigRoot);

        //        var camEl = new VRCameraComponent { HeightOffset = vrRig.PlayerHeight };
        //        AddComponentNode(camEl, rigRoot);

        //        //AddComponentNode(new VRControllerComponent { Hand = VRHand.Left }, rigRoot);
        //        //AddComponentNode(new VRControllerComponent { Hand = VRHand.Right }, rigRoot);
        //            vrRig.RunComponentLogic(); // this should get the root node assigned properly
        //            _componentToNode[comp] = rigRoot;
        //        break;
        //    }
        //    case Component comps:
        //        {
        //            var genericNode = new Node3D { Name = comp.Name };
        //            parent.AddChild(genericNode);
        //            _componentToNode[comps] = genericNode;
        //            break;
        //        }
        //}

        var compNode = comp.RunComponentLogic();
        if (compNode.GetParent() != null)
        {
            compNode.GetParent().RemoveChild(compNode);
        }
        parent.AddChild(compNode);
        _componentToNode[comp] = compNode;
    }

    private void OnComponentAdded(IComponent comp)
    {
        if (_elementToNode.TryGetValue(comp.ParentElement, out var parent))
        {
            AddComponentNode(comp, parent);
        }
    }

    private void OnComponentRemoved(IComponent comp)
    {
        if (_componentToNode.TryGetValue(comp, out var node))
        {
            node.QueueFree();
            _componentToNode.Remove(comp);
        }
    }
}