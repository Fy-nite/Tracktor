#nullable enable
using Godot;

public partial class PlayerAvatar : Node3D
{
    public long PeerId { get; set; }
    public bool IsLocalPlayer { get; set; }

    [Export] public float ClientMoveSpeed = 4.5f;

    private int _sequence;
    private int _lastServerProcessed;
    private NetRoot? _net;

    public override void _Ready()
    {
        _net = FindNetRoot();

        if (GetNodeOrNull("Mesh") == null)
        {
            // Minimal visible geometry for non-headless runs.
            var mesh = new MeshInstance3D { Name = "Mesh" };
            var box = new BoxMesh
            {
                Size = new Vector3(0.5f, 1.0f, 0.5f)
            };
            mesh.Mesh = box;
            mesh.Position = new Vector3(0, 0.5f, 0);
            AddChild(mesh);
        }
    }

    private static NetRoot? FindNetRoot(Node? start = null)
    {
        var node = start;
        while (node != null)
        {
            if (node is NetRoot nr)
                return nr;
            node = node.GetParent();
        }

        // Fallback: locate via group.
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

    public override void _PhysicsProcess(double delta)
    {
        if (!IsLocalPlayer)
            return;

        var dir = ReadMoveDir();
        var dt = (float)delta;

        // Local prediction: move immediately.
        if (dir.LengthSquared() > 0.0001f)
        {
            dir = dir.Normalized();
            GlobalPosition += dir * ClientMoveSpeed * dt;
        }

        // Send input to server for authority + replication.
        _sequence++;
        _net?.SubmitLocalInput(_sequence, dt, dir);
    }

    public void ApplyAuthoritative(Vector3 pos, int lastProcessedInput)
    {
        _lastServerProcessed = lastProcessedInput;

        // Simple reconciliation: snap if we drift too far.
        var drift = GlobalPosition.DistanceTo(pos);
        if (drift > 0.35f)
            GlobalPosition = pos;
        else
            GlobalPosition = GlobalPosition.Lerp(pos, 0.25f);
    }

    private static Vector3 ReadMoveDir()
    {
        // Uses Godot default actions (ui_left/right/up/down).
        var x = Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
        var z = Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up");
        return new Vector3(x, 0, z);
    }
}
