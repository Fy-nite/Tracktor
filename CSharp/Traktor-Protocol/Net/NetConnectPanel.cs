#nullable enable
using Godot;

public partial class NetConnectPanel : PanelContainer
{
    private NetRoot? _net;

    private LineEdit? _host;
    private SpinBox? _port;

    private Button? _startServer;
    private Button? _connect;
    private Button? _disconnect;

    public override void _Ready()
    {
        // HomeScreen layout: Control is root, NetRoot is sibling.
        _net = GetNodeOrNull<NetRoot>("../NetRoot");

        _host = GetNodeOrNull<LineEdit>("VBox/HostRow/Host");
        _port = GetNodeOrNull<SpinBox>("VBox/PortRow/Port");

        _startServer = GetNodeOrNull<Button>("VBox/Buttons/StartServer");
        _connect = GetNodeOrNull<Button>("VBox/Buttons/Connect");
        _disconnect = GetNodeOrNull<Button>("VBox/Buttons/Disconnect");

        if (_net == null || _host == null || _port == null || _startServer == null || _connect == null || _disconnect == null)
        {
            GD.PushWarning("NetConnectPanel: missing nodes or NetRoot");
            return;
        }

        _host.Text = _net.RemoteHost;
        _port.MinValue = 1;
        _port.MaxValue = 65535;
        _port.Step = 1;
        _port.Value = _net.PortInUse != 0 ? _net.PortInUse : _net.Port;

        _startServer.Pressed += () =>
        {
            _net.StartServerFromUi((int)_port.Value);
        };

        _connect.Pressed += () =>
        {
            _net.StartClientFromUi(_host.Text, (int)_port.Value);
        };

        _disconnect.Pressed += () =>
        {
            _net.StopNetworking();
        };
    }

    public override void _Process(double delta)
    {
        if (_net == null || _startServer == null || _connect == null || _disconnect == null)
            return;

        var online = Multiplayer.MultiplayerPeer != null;
        _disconnect.Disabled = !online;

        // Don’t allow switching modes while online.
        _startServer.Disabled = online;
        _connect.Disabled = online;
    }
}
