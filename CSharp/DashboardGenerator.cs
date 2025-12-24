namespace Trakction;

using Godot;
public partial class DashboardGenerator : Node3D 
{
    [Export] public NodePath ThisNode;
    public override void _Ready()
    {
        if (ThisNode != null)
        {
            var node = GetNode<Node3D>(ThisNode);
            node.AddChild(new Tracktor.CSharp.Construction.Dashboard().GenerateDashboardUI(this));
            GD.Print("Dashboard found node: " + node.Name);
        }
        else
        {
            GD.Print("Dashboard has no ThisNode set.");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
     if (Input.IsKeyPressed(Key.R))
             {
                 var node = GetNode(ThisNode);
                 foreach (var child in node.GetChildren())
                 {
                     node.RemoveChild(child);
                     child.QueueFree();
                 }
                 _Ready();
                 GD.Print("reloaded UI Dashboards.");
             }   
    }

    public override void _Process(double delta)
    {
        
    }
}