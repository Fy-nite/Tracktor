#nullable enable
using Godot;

public partial class NetDebugLabel : Label
{
    private NetRoot? _net;

    public override void _Ready()
    {
        // NetRoot is at ../../NetRoot from UI/NetDebug.
        _net = GetNodeOrNull<NetRoot>("../../NetRoot");
    }

    private static NetRoot? FindNetRoot(Node? start)
    {
        var node = start;
        while (node != null)
        {
            if (node is NetRoot nr)
                return nr;
            node = node.GetParent();
        }

        var tree = Godot.Engine.GetMainLoop() as SceneTree;
        if (tree == null)
            return null;

        foreach (var n in tree.GetNodesInGroup("net_root"))
        {
            if (n is NetRoot nr)
                return nr;
        }

        return null;
    }

    public override void _Process(double delta)
    {
        if (_net == null)
        {
            Text = "Net: (missing NetRoot)";
            return;
        }

        var mode = _net.IsServerMode ? "Server" : "Client";
        var peer = Multiplayer.MultiplayerPeer == null ? 0 : Multiplayer.GetUniqueId();
        var status = _net.ConnectionStatus;

        var line1 = $"Net: {mode} | Peer {peer} | {status}";
        var line2 = _net.IsServerMode
            ? $"Port: {_net.PortInUse}"
            : $"Remote: {_net.RemoteHost}:{_net.PortInUse}";

        Text = line1 + "\n" + line2;
    }
}
