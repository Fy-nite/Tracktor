#nullable enable
using Godot;

public partial class NetConnectPanel : PanelContainer
{
    private NetRoot? _net;

    private LineEdit? _host;
    private SpinBox? _port;

    private Control? _hostRow;
    private Control? _portRow;

    private Button? _startServer;
    private Button? _connect;
    private Button? _disconnect;
    private Button? _vr;

    public override void _Ready()
    {
        // NetRoot is at ../../NetRoot from UI/NetConnect.
        _net = GetNodeOrNull<NetRoot>("../../NetRoot");

        _hostRow = GetNodeOrNull<Control>("VBox/HostRow");
        _portRow = GetNodeOrNull<Control>("VBox/PortRow");

        _host = GetNodeOrNull<LineEdit>("VBox/HostRow/Host");
        _port = GetNodeOrNull<SpinBox>("VBox/PortRow/Port");

        _startServer = GetNodeOrNull<Button>("VBox/Buttons/StartServer");
        _connect = GetNodeOrNull<Button>("VBox/Buttons/Connect");
        _disconnect = GetNodeOrNull<Button>("VBox/Buttons/Disconnect");
        _vr = GetNodeOrNull<Button>("VBox/Buttons/VrButton");

        if (_net == null || _hostRow == null || _portRow == null || _host == null || _port == null || _startServer == null || _connect == null || _disconnect == null)
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
        _vr.Pressed += () =>
        {
            _net.StartVr();
        };
    }

    public override void _Process(double delta)
    {
        if (_net == null || _hostRow == null || _portRow == null || _startServer == null || _connect == null || _disconnect == null)
            return;

        var online = Multiplayer.MultiplayerPeer != null;

        // When connected/listening, get out of the way so you see the 3D view.
        _hostRow.Visible = !online;
        _portRow.Visible = !online;
        _startServer.Visible = !online;
        _connect.Visible = !online;
        _disconnect.Visible = online;

        _disconnect.Disabled = !online;
    }
}
