#nullable enable
using Godot;

namespace Traktion.CSharp.Construction;

public static class InspectorPanelService
{
    private const string CanvasLayerName = "InspectorUI";
    private const string PanelName = "InspectorPanel";

    private const string DefaultScenePath = "res://assets/Scenes/Creation/InspectorPanel2D.tscn";

    public static void Toggle(Node caller)
    {
        var root = caller.GetTree().CurrentScene;
        if (root == null)
            return;

        var layer = root.GetNodeOrNull<CanvasLayer>(CanvasLayerName);
        if (layer == null)
        {
            layer = new CanvasLayer { Name = CanvasLayerName };
            root.AddChild(layer);
        }

        var panel = layer.GetNodeOrNull<InspectorPanel2D>(PanelName);
        if (panel == null)
        {
            var scene = ResourceLoader.Load<PackedScene>(DefaultScenePath);
            if (scene == null)
            {
                GD.PrintErr($"InspectorPanelService: Could not load {DefaultScenePath}");
                return;
            }

            var inst = scene.Instantiate();
            panel = inst as InspectorPanel2D ?? inst.GetNodeOrNull<InspectorPanel2D>(".");
            if (panel == null)
            {
                GD.PrintErr("InspectorPanelService: InspectorPanel2D scene did not instantiate correctly.");
                return;
            }
            
            panel.Name = PanelName;
            layer.AddChild(panel);

            panel.Closed += () => panel.Visible = false;
        }

        panel.Visible = !panel.Visible;
        if (panel.Visible)
            panel.Rebuild();
    }
}
