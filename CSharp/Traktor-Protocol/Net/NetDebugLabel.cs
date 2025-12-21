#nullable enable
using Godot;

public partial class NetDebugLabel : Label
{
    private NetRoot? _net;

    public override void _Ready()
    {
        // HomeScreen: Label is under Control, NetRoot is a sibling.
        _net = GetNodeOrNull<NetRoot>("../NetRoot") ?? GetNodeOrNull<NetRoot>("/root/Control/NetRoot");
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
