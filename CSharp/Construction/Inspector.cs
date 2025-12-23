namespace trakction.CSharp.Construction;

using Godot;
using System.Collections.Generic;
public class Inspector
{
    public Node _slot;
    public string Name;
    public List<Node> LeftHand;
    public List<Node> RightHand;

    public Inspector(Node Slot)
    {
        _slot = Slot;
        GD.Print($"Created inspector for {Slot.Name}");
    }

    public Node GenerateInspectorUI()
    {
        return new Node();
    }
}