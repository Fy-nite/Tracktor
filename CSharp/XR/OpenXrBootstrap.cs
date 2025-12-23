#nullable enable
using System;
using Godot;

namespace Trakction.XR;

/// <summary>
/// Minimal OpenXR bootstrapper.
/// Attach this to a node in your startup scene to enable XR rendering.
/// </summary>
public partial class OpenXrBootstrap : Node
{
    [Signal] public delegate void XrEnabledEventHandler();

    [Export] public bool AutoStart { get; set; } = true;
    [Export] public string InterfaceName { get; set; } = "OpenXR";
    [Export] public bool DisableVsync { get; set; } = true;
    [Export] public bool ShowDiagnosticsOverlay { get; set; } = true;

    private CanvasLayer? _diagLayer;
    private Label? _diagLabel;

    public override void _Ready()
    {
        if (!AutoStart)
            return;

        TryEnableXr();

        if (ShowDiagnosticsOverlay && Engine.IsEditorHint() == false)
            CreateDiagnosticsOverlay();
    }

    private void CreateDiagnosticsOverlay()
    {
        try
        {
            var root = GetTree().Root;
            if (root == null)
                return;

            // Only create once
            if (_diagLayer != null)
                return;

            _diagLayer = new CanvasLayer();
            _diagLayer.Name = "XrDiagnostics";
            var lbl = new Label();
            lbl.Name = "XrDiagLabel";
            lbl.Text = "XR: initializing...";
            lbl.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0));
            lbl.SetPosition(new Vector2(8, 8));
            _diagLayer.AddChild(lbl);
            root.AddChild(_diagLayer);
            _diagLabel = lbl;
        }
        catch { }
    }

    private void UpdateDiagnosticsOverlay(string text)
    {
        try
        {
            if (_diagLabel != null)
                _diagLabel.Text = text;
        }
        catch { }
    }

    public bool TryEnableXr()
    {
        GD.Print("Trying to enable XR");
        UpdateDiagnosticsOverlay("XR: trying to enable...");
        var xrInterface = XRServer.FindInterface(InterfaceName);
        if (xrInterface == null)
        {
            GD.PushWarning($"XR interface '{InterfaceName}' not found. Is the OpenXR plugin enabled?");
            UpdateDiagnosticsOverlay($"XR: interface '{InterfaceName}' not found");
            return false;
        }

        // Dump basic interface state for diagnostics
        DumpXrInterfaceState(xrInterface);

        if (!xrInterface.IsInitialized())
        {
            var ok = xrInterface.Initialize();
            if (!ok)
            {
                GD.PushWarning($"Failed to initialize XR interface '{InterfaceName}'.");
                UpdateDiagnosticsOverlay($"XR: failed to initialize '{InterfaceName}'");
                return false;
            }
        }

        UpdateDiagnosticsOverlay($"XR: interface '{InterfaceName}' initialized");

        if (DisableVsync)
        {
            // Recommended when XR is active; the XR runtime drives presentation timing.
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        }

        // Try marking UseXR on several candidate viewports. Some setups may use a different viewport
        // than the one returned by GetViewport(); mark root, node viewport and any child viewports.
        var marked = EnsureUseXrMarked();
        if (marked)
        {
            GD.Print($"XR enabled via '{InterfaceName}' (viewport flagged).");
            UpdateDiagnosticsOverlay($"XR enabled: {InterfaceName}");
            // Emit a signal so other scripts (like HomeScreen) can wait for XR readiness before creating XR-only nodes.
            EmitSignal(nameof(XrEnabled));
            return true;
        }

        // If we failed, dump viewports for deeper diagnostics.
        GD.PushWarning("Failed to mark any viewport with UseXR; dumping viewports for diagnostics.");
        UpdateDiagnosticsOverlay("XR: no viewport flagged with UseXR");
        DumpViewports();

        return false;
    }

    private bool EnsureUseXrMarked()
    {
        var any = false;

        try
        {
            var tree = GetTree();
            if (tree != null)
            {
                var root = tree.Root;
                if (root != null)
                {
                    root.UseXR = true;
                    GD.Print("Marked root viewport UseXR = true");
                    any = true;

                    // Mark any child viewports too.
                    foreach (var child in root.GetChildren())
                    {
                        if (child is Viewport vp)
                        {
                            vp.UseXR = true;
                            GD.Print($"Marked child viewport '{vp.Name}' UseXR = true");
                            any = true;
                        }
                    }
                }

                // Mark viewports inside the currently loaded scene (recursively)
                var currentScene = tree.CurrentScene;
                if (currentScene != null)
                {
                    GD.Print($"Attempting to mark viewports in current scene '{currentScene.Name}'");
                    var found = MarkViewportsInNodeRecursive(currentScene);
                    if (found)
                    {
                        GD.Print("Marked one or more viewports inside current scene.");
                        any = true;
                    }
                }
            }

            // Also try the viewport this node belongs to (if different).
            var nodeVp = GetViewport();
            if (nodeVp != null && (tree == null || nodeVp != tree.Root))
            {
                nodeVp.UseXR = true;
                GD.Print("Marked node's viewport UseXR = true");
                any = true;
            }

            // As a last resort, scan the SceneTree root's Node children recursively for any Viewport nodes
            try
            {
                var rt = GetTree()?.Root;
                if (rt != null)
                {
                    foreach (var child in rt.GetChildren())
                    {
                        if (child is Node n)
                        {
                            var found = MarkViewportsInNodeRecursive(n);
                            if (found)
                                any = true;
                        }
                    }
                }
            }
            catch { /* ignore */ }
        }
        catch (Exception e)
        {
            GD.PrintErr($"EnsureUseXrMarked exception: {e.Message}");
        }

        return any;
    }

    // Recursively search a node and its children for Viewport nodes and set UseXR=true on them.
    private bool MarkViewportsInNodeRecursive(Node node)
    {
        var any = false;
        try
        {
            if (node is Viewport vp)
            {
                vp.UseXR = true;
                GD.Print($"Marked viewport '{vp.Name}' UseXR = true (recursive)");
                any = true;
            }

            foreach (var childObj in node.GetChildren())
            {
                if (childObj is Node child)
                {
                    if (MarkViewportsInNodeRecursive(child))
                        any = true;
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"MarkViewportsInNodeRecursive error on node '{node?.Name}': {e.Message}");
        }

        return any;
    }

    private void DumpXrInterfaceState(object xrInterface)
    {
        try
        {
            // We don't rely on specific XR interface CLR types to avoid binding issues.
            // Print whether Initialize was called and show a simple ToString.
            GD.Print($"XR Interface object: {xrInterface}");
            // Try to reflect common members if available
            var type = xrInterface.GetType();
            var initProp = type.GetMethod("IsInitialized");
            if (initProp != null)
            {
                var initialized = initProp.Invoke(xrInterface, null);
                GD.Print($"XR interface IsInitialized: {initialized}");
            }

            // Print basic type info and available methods/properties for diagnostics
            GD.Print($"XR Interface CLR type: {type.FullName}");

            var props = type.GetProperties();
            if (props.Length > 0)
            {
                GD.Print("XR Interface properties:");
                foreach (var p in props)
                {
                    try
                    {
                        var val = p.GetValue(xrInterface);
                        GD.Print($"  {p.Name} = {val}");
                    }
                    catch { GD.Print($"  {p.Name} (unreadable)"); }
                }
            }

            var methods = type.GetMethods();
            GD.Print($"XR Interface methods (count={methods.Length}):");
            for (int i = 0; i < methods.Length && i < 50; i++)
            {
                GD.Print($"  {methods[i].Name}");
            }

            // Try to call a 'GetName' or 'Name' if present
            var getNameMethod = type.GetMethod("GetName") ?? type.GetMethod("GetID") ?? type.GetMethod("Name");
            if (getNameMethod != null)
            {
                try
                {
                    var nameVal = getNameMethod.Invoke(xrInterface, null);
                    GD.Print($"XR Interface reported name: {nameVal}");
                }
                catch { /* ignore */ }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"DumpXrInterfaceState error: {e.Message}");
        }
    }

    private void DumpViewports()
    {
        try
        {
            var tree = GetTree();
            if (tree == null)
            {
                GD.PrintErr("DumpViewports: SceneTree is null");
                UpdateDiagnosticsOverlay("XR: no SceneTree");
                return;
            }

            var root = tree.Root;
            if (root == null)
            {
                GD.PrintErr("DumpViewports: Root viewport is null");
                UpdateDiagnosticsOverlay("XR: no root viewport");
                return;
            }

            GD.Print($"DumpViewports: Root.UseXR = {root.UseXR}");
            UpdateDiagnosticsOverlay($"Root.UseXR = {root.UseXR}");

            foreach (var child in root.GetChildren())
            {
                    if (child is Viewport vp)
                    {
                        GD.Print($"Child viewport '{vp.Name}': UseXR={vp.UseXR}");
                        UpdateDiagnosticsOverlay($"Child viewport '{vp.Name}': UseXR={vp.UseXR}");
                    }
                else
                {
                    GD.Print($"Child node '{child}': type={child.GetType()}");
                }
            }

            var nodeVp = GetViewport();
            if (nodeVp != null)
            {
                GD.Print($"Node's viewport '{nodeVp.Name}': UseXR={nodeVp.UseXR}");
                UpdateDiagnosticsOverlay($"Node viewport '{nodeVp.Name}': UseXR={nodeVp.UseXR}");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"DumpViewports exception: {e.Message}");
            UpdateDiagnosticsOverlay($"XR DumpViewports error: {e.Message}");
        }
    }
}
