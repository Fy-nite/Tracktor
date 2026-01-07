using Godot;
using V12.Extensions;

namespace V12.Components
{
	public partial class DesktopPlayer : NodeComponent
	{
		[InspectorExpose]
		public Vector3 Position { get; set; }
		[InspectorExpose]
		public Vector3 Rotation { get; set; }

		[InspectorExpose]
		public Vector3 Scale { get; set; }
		public bool IsCameralocal { get; set; } = true; // set this to false to disable camera control for non -local players
        private Camera3D _camera;
		public Camera3D camera
		{
			get { return _camera; }
			set { _camera = value; }
		}

		[InspectorExpose]
		public float MoveSpeed { get; set; } = 4.0f;

		[InspectorExpose]
		public float MouseSensitivity { get; set; } = 0.0035f;

		private DesktopPlayerBody? body;
		private Vector3 velocity = Vector3.Zero;
		private float gravity = -24f;
		private float jumpSpeed = 6f;
		private float yaw = 0f;
		private float pitch = 0f;
		private bool wasSpacePressed = false;
		private bool isMouseLocked = true;

		public override string Category => "Mesh";
		public override string Description => "Desktop player";


		public DesktopPlayer()
		{
			// defer creation of sceneElement until RunComponentLogic so object initializer values are applied
		}
		public DesktopPlayer(Vector3 position, Vector3 rotation, Vector3 scale)
		{
			Position = position; // shoulb be the starting position of the player
			Rotation = rotation;
			Scale = scale;
		}
		public override Control GenerateUI()
		{
			return ComponentUIGenerator.GenerateAutoUI(this);
		}
		public override void OnUpdate(double delta)
		{
		if (IsCameralocal)
		{
			if (_camera != null)
			{
				_camera.Current = true;
			}
			// only run if camera is local
			// run input/control logic for local camera
			// fall through to input handling below
		}
		if (IsCameralocal)
			{
				base.OnUpdate(delta);
				if (body == null) return;

				// Toggle mouse capture with Escape
				if (isMouseLocked && Input.IsActionJustPressed("ui_cancel"))
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
					isMouseLocked = false;
				}
				else if (!isMouseLocked && Input.IsActionJustPressed("ui_cancel"))
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
					isMouseLocked = true;
				}

				// Mouse look (only if captured)
				if (isMouseLocked)
				{
					var mouseDelta = body.MouseDelta;
					body.ResetMouseDelta();
					yaw -= mouseDelta.X * MouseSensitivity;
					pitch -= mouseDelta.Y * MouseSensitivity;
					pitch = Mathf.Clamp(pitch, -1.3f, 1.3f); // clamp pitch to avoid flipping
				}

				// Keyboard arrow keys for camera rotation (alternative to mouse)
				float lookSpeed = 1.5f;
				if (Input.IsKeyPressed(Key.Up)) pitch -= lookSpeed * (float)delta;
				if (Input.IsKeyPressed(Key.Down)) pitch += lookSpeed * (float)delta;
				if (Input.IsKeyPressed(Key.Left)) yaw -= lookSpeed * (float)delta;
				if (Input.IsKeyPressed(Key.Right)) yaw += lookSpeed * (float)delta;

				// Apply rotations
				body.Rotation = new Vector3(0, yaw, 0);
				_camera.Rotation = new Vector3(pitch, 0, 0);

				// Movement input
				Vector3 inputDir = Vector3.Zero;
				var forward = -body.GlobalTransform.Basis.Z;
				var right = body.GlobalTransform.Basis.X;
				if (Input.IsKeyPressed(Key.W)) inputDir += forward;
				if (Input.IsKeyPressed(Key.S)) inputDir -= forward;
				if (Input.IsKeyPressed(Key.A)) inputDir -= right;
				if (Input.IsKeyPressed(Key.D)) inputDir += right;

				// Check if a gamepad is connected
				if (Input.GetConnectedJoypads().Count > 0)
				{
					// Controller input
					Vector2 leftStick = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
					inputDir += -forward * leftStick.Y; // forward/back
					inputDir += right * leftStick.X;  // left/right

					// Controller right stick for camera rotation
					Vector2 rightStick = Input.GetVector("turn_left", "turn_right", "look_up", "look_down");
					yaw += rightStick.X * lookSpeed * (float)delta;
					pitch -= rightStick.Y * lookSpeed * (float)delta;
					pitch = Mathf.Clamp(pitch, -1.3f, 1.3f);
				}

				// Normalize input direction
				if (inputDir != Vector3.Zero)
				{
					inputDir = inputDir.Normalized();
				}

				// Physics: apply gravity
				if (!body.IsOnFloor())
				{
					velocity.Y += gravity * (float)delta;
				}
				else
				{
					velocity.Y = 0; // reset vertical velocity on floor
					bool isSpacePressed = Input.IsKeyPressed(Key.Space);
					if (isSpacePressed && !wasSpacePressed)
					{
						velocity.Y = jumpSpeed;
					}
					wasSpacePressed = isSpacePressed;
				}

				// Horizontal velocity
				velocity.X = inputDir.X * MoveSpeed;
				velocity.Z = inputDir.Z * MoveSpeed;

				// Move using CharacterBody3D physics
				body.Velocity = velocity;
				body.MoveAndSlide();
				// Update velocity after MoveAndSlide
				velocity = body.Velocity;
			}
		}
		public override void OnAttach()
		{

			if (sceneElement == null && IsCameralocal)
			{
				// create a simple player body with a camera and collision shape
				var b = new DesktopPlayerBody()
				{
					Position = Position,
					Rotation = Rotation,
					Scale = Scale,
					Name = "DesktopPlayer",
				};

				var collision = new CollisionShape3D();
				collision.Shape = new CapsuleShape3D { Radius = 0.5f, Height = 1.2f };
				b.AddChild(collision);

				_camera = new Camera3D { Name = "PlayerCamera", Position = new Vector3(0, 1.6f, 0) };
				b.AddChild(_camera);

				sceneElement = b;
				GD.Print("[DesktopPlayer] Created DesktopPlayer CharacterBody3D in OnAttach");
			}

			base.OnAttach(); // add to parent

			// Cache typed reference
			body = sceneElement as DesktopPlayerBody;

			// Capture mouse for look
			Input.MouseMode = Input.MouseModeEnum.Captured;
			isMouseLocked = true;
		}

		public override void OnAwake()
		{
			base.OnAwake();
		}

		private partial class DesktopPlayerBody : CharacterBody3D
		{
			public Vector2 MouseDelta { get; private set; } = Vector2.Zero;

			public DesktopPlayerBody()
			{
				SetProcessUnhandledInput(true);
				GD.Print("[DesktopPlayerBody] Initialized and ready to capture mouse input");
			}

			public override void _Ready()
			{
				GD.Print("[DesktopPlayerBody] _Ready called");
			}

			public override void _UnhandledInput(InputEvent @event)
			{
				GD.Print("[DesktopPlayerBody] _UnhandledInput called");
				if (@event is InputEventMouseMotion mouseEvent)
				{
					MouseDelta += mouseEvent.Relative;
					GD.Print($"[DesktopPlayerBody] MouseDelta updated to {MouseDelta}");
				}
			}

			public void ResetMouseDelta()
			{
				MouseDelta = Vector2.Zero;
			}
		}
	}
}
