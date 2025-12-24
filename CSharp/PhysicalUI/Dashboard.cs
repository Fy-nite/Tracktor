using Godot;

namespace Tracktor.CSharp.Construction;

public partial class Dashboard : Node3D
{
    public string Title { get; set; } = "Dashboard";
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public Node GenerateDashboardUI(Node parent)
    {
        
        var dashboard = new Dashboard();
        dashboard.Title = "Player Dashboard";
        dashboard.Width = Width;
        dashboard.Height = Height;
        dashboard.Position = new Vector3(0, 0, -3);
        var pointer = new CsgBox3D();
        pointer.Name = "DashboardPointer";
        pointer.Position = new Vector3(0, 0, 0);
        
        dashboard.AddChild(pointer);
        var Panel = new CsgBox3D();
        Panel.Name = "DashboardPanelBacking";
        GD.Print("meows");
        Panel.Size = new Vector3(1, 1, 0.1f);
        
        dashboard.AddChild(Panel);

        var light = new SpotLight3D();
        light.LightEnergy = 0.5f;
        light.SpotRange = 10f;
        light.Position = new Vector3(0, 5, 0);
        dashboard.AddChild(light);

        return dashboard;
    }
}