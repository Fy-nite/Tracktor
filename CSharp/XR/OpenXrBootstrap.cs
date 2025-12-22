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
    [Export] public bool AutoStart { get; set; } = true;
    [Export] public string InterfaceName { get; set; } = "OpenXR";
    [Export] public bool DisableVsync { get; set; } = true;

    public override void _Ready()
    {
        if (!AutoStart)
            return;

        TryEnableXr();
    }

    public override void _Process(double delta)
    {
        if (GetTree().Root.GetViewport().UseXR)
            return;

        // Retry if not set
        TryEnableXr();
    }

    public bool TryEnableXr()
    {
        GD.Print("Trying to enable XR");
        var xrInterface = XRServer.FindInterface(InterfaceName);
        if (xrInterface == null)
        {
            GD.PushWarning($"XR interface '{InterfaceName}' not found. Is the OpenXR plugin enabled?");
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
                return false;
            }
        }

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
            return true;
        }

        // If we failed, dump viewports for deeper diagnostics.
        GD.PushWarning("Failed to mark any viewport with UseXR; dumping viewports for diagnostics.");
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
            }

            // Also try the viewport this node belongs to (if different).
            var nodeVp = GetViewport();
            if (nodeVp != null && (tree == null || nodeVp != tree.Root))
            {
                nodeVp.UseXR = true;
                GD.Print("Marked node's viewport UseXR = true");
                any = true;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"EnsureUseXrMarked exception: {e.Message}");
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
                return;
            }

            var root = tree.Root;
            if (root == null)
            {
                GD.PrintErr("DumpViewports: Root viewport is null");
                return;
            }

            GD.Print($"DumpViewports: Root.UseXR = {root.UseXR}");

            foreach (var child in root.GetChildren())
            {
                    if (child is Viewport vp)
                    {
                        GD.Print($"Child viewport '{vp.Name}': UseXR={vp.UseXR}");
                    }
                else
                {
                    GD.Print($"Child node '{child}': type={child.GetType()}");
                }
            }

            var nodeVp = GetViewport();
            if (nodeVp != null)
                GD.Print($"Node's viewport '{nodeVp.Name}': UseXR={nodeVp.UseXR}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"DumpViewports exception: {e.Message}");
        }
    }
}
