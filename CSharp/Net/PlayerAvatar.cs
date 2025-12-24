﻿#nullable enable
using Godot;

public partial class PlayerAvatar : CharacterBody3D
{
    public long PeerId { get; set; }
    public bool IsLocalPlayer { get; set; }

    [Export] public float MoveSpeed = 4.5f;
    [Export] public float Acceleration = 35.0f;
    [Export] public float AirAcceleration = 10.0f;
    [Export] public float JumpVelocity = 4.8f;

    [Export] public float GamepadMoveDeadzone = 0.18f;
    [Export] public float GamepadLookDeadzone = 0.22f;
    [Export] public float GamepadLookYawSpeed = 3.2f; // rad/sec at full stick

    [Export] public bool CaptureMouseForLocalPlayer = true;
    [Export] public float MouseLookSensitivity = 0.0032f;

    [Export] public NodePath LookPivotPath = new();
    [Export] public float MaxPitchDegrees = 80.0f;

    private NetRoot? _net;
    private Node3D? _lookPivot;
    private Camera3D? _camera;
    private CanvasLayer? _ui;

    private bool _mouseCaptured;
    private bool _userManuallyReleased;
    private int _sendCounter;
    private int _localWarmupFrames;

    private const float MaxSendPositionMagnitude = 5000.0f;

    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        _net = FindNetRoot(this);
        AddToGroup("net_sync");
        _ui = (CanvasLayer)GetTree().Root.FindChild("UI");
        if (_ui != null)
            _ui.Visible = true;
        _lookPivot = !LookPivotPath.IsEmpty ? GetNodeOrNull<Node3D>(LookPivotPath) : GetNodeOrNull<Node3D>("LookPivot");
        _camera = _lookPivot?.GetNodeOrNull<Camera3D>("Camera3D") ?? GetNodeOrNull<Camera3D>("LookPivot/Camera3D");

        if (GetNodeOrNull("Mesh") == null)
        {
            var mesh = new MeshInstance3D { Name = "Mesh" };
            mesh.Mesh = new BoxMesh { Size = new Vector3(0.5f, 1.0f, 0.5f) };
            mesh.Position = new Vector3(0, 0.5f, 0);
            AddChild(mesh);
        }
        if (GetNodeOrNull("NameTag") == null)
        {
            var nameTag = new MeshInstance3D() { Name = "NameTag" };
            var textMesh = new TextMesh();
            nameTag.Position = new Vector3(0, 2.2f, 0);
            textMesh.Text = $"Player {PeerId}";
            nameTag.Scale = new Vector3(0.15f, 0.15f, 0.15f);
            nameTag.Mesh = textMesh;
            nameTag.AddChild(nameTag);
            AddChild(nameTag);
        }

        if (GetNodeOrNull<CollisionShape3D>("Collision") == null)
        {
            var col = new CollisionShape3D { Name = "Collision" };
            col.Shape = new CapsuleShape3D { Radius = 0.25f, Height = 1.0f };
            AddChild(col);
        }
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
        {
            if (_camera != null)
                _camera.Current = false;
            return;
        }

        EnsureLocalFirstPersonActive();

        var dt = (float)delta;
        if (dt < 0) dt = 0;
        if (dt > 0.1f) dt = 0.1f; // clamp to avoid huge jumps

        ApplyLook(dt);

        var moveWorldDir = ReadMoveWorldDir();
        ApplyLocalMovement(moveWorldDir, dt);

        // Send position update to server periodically (after a short warm-up).
        _sendCounter++;
        if (_localWarmupFrames < 10)
        {
            _localWarmupFrames++;
            return;
        }

        // 12 Hz-ish at 60fps
        if (_sendCounter % 5 != 0)
            return;

        var pos = GlobalPosition;
        if (!IsFinite(pos) || pos.Length() > MaxSendPositionMagnitude)
            return;

        _net?.SendPlayerPosition(pos, GlobalRotation);
        

    }
    public override void _Process(double delta)
    {
        if (!IsLocalPlayer)
            return;

        // Update nametag to face camera.
        var tree = GetTree() as SceneTree;
        var cam = _camera;
        var nameTag = GetNodeOrNull<Node3D>("NameTag");
        if (cam != null && nameTag != null)
        {
            var toCam = cam.GlobalPosition - nameTag.GlobalPosition;
            toCam.Y = 0;
            if (toCam.LengthSquared() > 1e-6f)
            {
                toCam = toCam.Normalized();
                var targetRot = Mathf.Atan2(toCam.X, toCam.Z);
                nameTag.GlobalRotation = new Vector3(0, targetRot, 0);
            }
        }
        // Toggle mouse capture with escape.
        // this is stupid. why doesn't it just update properly. 
        // TODO: fix this fucking bullshit at somepoint...
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (!_mouseCaptured)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                _mouseCaptured = false;
                _userManuallyReleased = true;
                if (_ui != null) _ui.Visible = true;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _mouseCaptured = true;
                _userManuallyReleased = false;
                if (_ui != null) _ui.Visible = false;
            }
            return;
        }

        // // Auto-capture only if not manually released.
        // if (!_mouseCaptured && !_userManuallyReleased)
        // {
        //     Input.MouseMode = Input.MouseModeEnum.Captured;
        //     _mouseCaptured = true;
        //     if (_ui != null) _ui.Visible = false;
        // }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsLocalPlayer)
            return;

        if (@event is InputEventMouseMotion mm && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= mm.Relative.X * MouseLookSensitivity;

            if (_lookPivot != null)
            {
                _pitch -= mm.Relative.Y * MouseLookSensitivity;
                var maxPitch = Mathf.DegToRad(MaxPitchDegrees);
                _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
            }

            ApplyLookRotation();
        }
    }

    private Vector3 ReadMoveWorldDir()
    {
        // Uses Godot default UI actions (these are usually mapped to keyboard + dpad + left stick).
        var v2 = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down", GamepadMoveDeadzone);

        // Godot 3D: adjust for desired forward direction.
        var local = new Vector3(v2.X, 0, v2.Y);
        if (local.LengthSquared() < 1e-6f)
            return Vector3.Zero;

        // Rotate movement by our yaw so stick forward moves "forward".
        var world = local.Rotated(Vector3.Up, _yaw);
        return world.Normalized();
    }

    private void ApplyLocalMovement(Vector3 moveWorldDir, float dt)
    {
        var gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

        var v = Velocity;
        v.Y -= gravity * dt;

        var accel = IsOnFloor() ? Acceleration : AirAcceleration;
        v.X = Mathf.MoveToward(v.X, moveWorldDir.X * MoveSpeed, accel * dt);
        v.Z = Mathf.MoveToward(v.Z, moveWorldDir.Z * MoveSpeed, accel * dt);

        if (IsOnFloor() && WantsJump())
            v.Y = JumpVelocity;

        Velocity = v;
        
        MoveAndSlide();
    }

    private bool WantsJump()
    {
        if (Input.IsActionJustPressed("ui_accept"))
            return true;

        // Common controller face buttons.
        foreach (var id in Input.GetConnectedJoypads())
        {
            if (Input.IsJoyButtonPressed(id, JoyButton.A))
                return true;
        }

        return false;
    }

    private void ApplyLook(float dt)
    {
        var padId = GetPrimaryJoypadId();
        if (padId < 0)
            return;

        var lookX = Input.GetJoyAxis(padId, JoyAxis.RightX);
        var lookY = Input.GetJoyAxis(padId, JoyAxis.RightY);

        lookX = ApplyDeadzone(lookX, GamepadLookDeadzone);
        lookY = ApplyDeadzone(lookY, GamepadLookDeadzone);

        if (Mathf.IsZeroApprox(lookX) && Mathf.IsZeroApprox(lookY))
            return;

        _yaw += lookX * GamepadLookYawSpeed * dt;

        if (_lookPivot != null)
        {
            _pitch -= lookY * GamepadLookYawSpeed * dt;
            var maxPitch = Mathf.DegToRad(MaxPitchDegrees);
            _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);
        }

        ApplyLookRotation();
    }

    private void EnsureLocalFirstPersonActive()
    {
        if (_camera != null)
            _camera.Current = true;

        if (!CaptureMouseForLocalPlayer)
            return;

        // Toggle mouse capture with escape.
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (_mouseCaptured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _mouseCaptured = false;
                _userManuallyReleased = true;
                if (_ui != null) _ui.Visible = false;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                _mouseCaptured = true;
                _userManuallyReleased = false;
                if (_ui != null) _ui.Visible = true;
            }
            return;
        }

        // Auto-capture only if not manually released.
        if (!_mouseCaptured && !_userManuallyReleased)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _mouseCaptured = true;
            if (_ui != null) _ui.Visible = false;
        }
    }

    private static bool IsFinite(Vector3 v)
        => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private void ApplyLookRotation()
    {
        Rotation = new Vector3(0, _yaw, 0);
        if (_lookPivot != null)
            _lookPivot.Rotation = new Vector3(_pitch, 0, 0);
    }

    private static float ApplyDeadzone(float v, float dz)
    {
        if (Mathf.Abs(v) < dz)
            return 0;
        return (Mathf.Abs(v) - dz) / (1.0f - dz) * Mathf.Sign(v);
    }

    private static int GetPrimaryJoypadId()
    {
        var pads = Input.GetConnectedJoypads();
        return pads.Count > 0 ? pads[0] : -1;
    }
}
