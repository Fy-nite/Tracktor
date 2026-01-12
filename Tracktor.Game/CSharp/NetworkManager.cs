#if GODOT
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using V12;
using V12.Headless;

// NetworkManager: encapsulates WorldClient/WorldServer usage and remote world lifecycle
public class NetworkManager
{
    private V12Engine _engine;
    private Action<Action> _runOnMainThread;
    private Action<bool> _updateUi;
    private Node3D _owner;

    private V12.Headless.WorldClient? _client;
    private V12.Headless.WorldServer? _server;
    private List<V12World> _networkWorlds = new();

    public event Action<V12World, int>? OnWorldAdded;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public NetworkManager(V12Engine engine, Action<Action> runOnMainThread, Action<bool> updateUi, Node3D owner)
    {
        _engine = engine;
        _runOnMainThread = runOnMainThread;
        _updateUi = updateUi;
        _owner = owner;
    }

    public void StartLocalServer(int port, Func<V12World> hostWorldFactory)
    {
        _server = new V12.Headless.WorldServer(port, hostWorldFactory);
    }

    public void ConnectToAddress(string address, int port)
    {
        if (_client != null) return;
        _client = new V12.Headless.WorldClient(address, port);
        WireHandlers(_client);

        _ = Task.Run(async () =>
        {
            try
            {
                await _client.ConnectAsync();
                // notify UI that we have an active connection
                try { _runOnMainThread(() => { _updateUi(true); OnConnected?.Invoke(); }); } catch { }
                await _client.RequestWorldAsync();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[NetworkManager] Network error: {ex.Message}");
                try { _runOnMainThread(() => { _updateUi(false); }); } catch { }
            }
        });
    }

    public void ConnectToLocalHost() => ConnectToAddress("127.0.0.1", 7777);

    public void Disconnect()
    {
        if (_client == null && _networkWorlds.Count == 0) return;

        try
        {
            foreach (var rw in _networkWorlds.ToList())
            {
                try
                {
                    if (rw.IsActive) rw.Deactivate();
                    if (rw.Viewport != null && rw.Viewport.GetParent() == _owner)
                    {
                        _owner.RemoveChild(rw.Viewport);
                    }
                    _engine.Worlds.Remove(rw);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[NetworkManager] Error removing remote world '{rw?.Name}': {ex.Message}");
                }
            }
            _networkWorlds.Clear();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[NetworkManager] Error cleaning up remote worlds: {ex.Message}");
        }

        try { _client?.Dispose(); } catch { }
        _client = null;

        try { _runOnMainThread(() => { _updateUi(false); OnDisconnected?.Invoke(); }); } catch { }
    }

    private void WireHandlers(V12.Headless.WorldClient client)
    {
        client.OnFullWorldReceived += (engineWorld) =>
        {
            GD.Print($"[NetworkManager] Received full engine world '{engineWorld?.Name}'");
            if (engineWorld == null) return;
            try
            {
                // Use the deserialized V12World directly instead of creating a new one and copying fields.
                var remote = engineWorld;
                if (string.IsNullOrEmpty(remote.Name)) remote.Name = "RemoteWorld";

                _runOnMainThread(() =>
                {
                    _engine.AddWorld(remote, _owner);
                    _engine.ActivateWorld(_engine.Worlds.Count - 1, _owner);
                    _networkWorlds.Add(remote);
                    _updateUi(true);
                    OnWorldAdded?.Invoke(remote, _engine.Worlds.Count - 1);
                });
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[NetworkManager] Error processing engine world: {ex.Message}");
            }
        };

        client.OnElementReceived += (elem) =>
        {
            _runOnMainThread(() =>
            {
                try
                {
                    GD.Print($"[NetworkManager][Deferred] Incremental element received: {elem?.Name}");
                    var remoteWorld = _engine.Worlds.FirstOrDefault(w => !w.IsLocalOnly && w.Name != "LocalWorld");
                    if (remoteWorld != null)
                    {
                        var existing = remoteWorld.RootElement.FindChild(elem.Name);
                        if (existing != null)
                        {
                            existing.Description = elem.Description;
                            existing.Type = elem.Type;
                            existing.Level = elem.Level;
                            existing.Position = elem.Position;
                            existing.Rotation = elem.Rotation;
                            existing.Scale = elem.Scale;
                        }
                        else
                        {
                            remoteWorld.RootElement.AddChild(elem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[NetworkManager][Deferred] Error handling incremental element: {ex.Message}");
                }
            });
        };
    }
}
#endif