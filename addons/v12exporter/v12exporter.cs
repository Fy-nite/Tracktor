#if TOOLS
using Godot;
using System;
using V12.Tools;

[Tool]
public partial class v12exporter : EditorPlugin
{
    PackedScene MainPanel = ResourceLoader.Load<PackedScene>("res://addons/v12exporter/main_panel.tscn");
    Control MainPanelInstance;
    public override void _EnterTree()
	{
        MainPanelInstance = (Control)MainPanel.Instantiate();
        // Add the main panel to the editor's main viewport.
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(MainPanelInstance);
        MainPanelInstance.GetChild<Button>(0).Pressed += ExportScene;
        // Hide the main panel. Very much required.
        _MakeVisible(false);
    }
    public void ExportScene()
    {
        if (EditorInterface.Singleton.GetEditedSceneRoot() != null)
        {
            var node = EditorInterface.Singleton.GetEditedSceneRoot();
            GD.Print("Exporting scene: " + node.Name);
            GD.Print(SceneToElementExporter.ExportSceneToCode(EditorInterface.Singleton.GetEditedSceneRoot()));
        }
    }
    public override void _ExitTree()
    {
        if (MainPanelInstance != null)
        {
            MainPanelInstance.QueueFree();
        }
    }
    public override bool _HasMainScreen()
    {
        return true;
    }

    public override void _MakeVisible(bool visible)
    {
        if (MainPanelInstance != null)
        {
            MainPanelInstance.Visible = visible;
        }
    }
    public override string _GetPluginName()
    {
        return "Finite V12 Tools";
    }

    public override Texture2D _GetPluginIcon()
    {
        // Must return some kind of Texture for the icon.
        return EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
    }
}

#endif
