using Godot;
using System;

public partial class character : CharacterBody3D
{
	[ExportCategory("Character")]
	[Export]
	float baseSpeed = 3.0f;
	[Export]
	float sprintSpeed = 6.0f;
	[Export]
	float crouchSpeed = 1.0f;

	[Export]
	float acceleration = 10.0f;
	[Export]
	float jumpVelocity = 4.5f;
	[Export]
	float mouseSensitivity = 0.005f;
	[Export]
	bool immobile = false;
	[Export(PropertyHint.File)]
	public string defaultReticle ;
	[Export]
	Vector3 initial_facing_direction = Vector3.Zero;

	[ExportGroup("Nodes")]
	[Export]
	Node3D HEAD ;
	[Export]
	Camera3D CAMERA ;
	[Export]
	AnimationPlayer HEADBOB_ANIMATION ;
	[Export]
	AnimationPlayer JUMP_ANIMATION ;
	[Export]
	AnimationPlayer CROUCH_ANIMATION ;
	[Export]
	CollisionShape3D COLLISION_MESH ;
	[Export]
	ShapeCast3D CEILING_DETECTION ;

	[ExportGroup("Controls")]
	[Export]
	String JUMP = "jump" ;
	[Export]
	String LEFT = "left" ;
	[Export]
	String RIGHT = "right" ;
	[Export]
	String FORWARD = "forward" ;
	[Export]
	String BACKWARD = "backward" ;
	[Export]
	String PAUSE ;
	[Export]
	String CROUCH = "crouch" ;
	[Export]
	String SPRINT = "sprint" ;

	[ExportGroup("Feature Settings")]
	[Export]
	bool jumpingEnabled = true ;
	[Export]
	bool inAirMomentum = true ;
	[Export]
	bool motionSmoothing = true ;
	[Export]
	bool sprintEnabled = true ;
	[Export]
	bool crouchEnabled = true ;
	[Export(PropertyHint.Enum, "Hold to Crouch,Toggle Crouch")]
	public int crouchMode = 0 ;
	[Export(PropertyHint.Enum, "Hold to Sprint,Toggle Sprint")]
	public int sprintMode = 0 ;
	[Export]
	bool dynamicFOV = true ;
	[Export]
	bool continuousJumping = true ;
	[Export]
	bool viewBobbing = true ;
	[Export]
	bool jumpAnimation = true ;

	// Member variables
	float speed ;
	float currentSpeed = 0.0f ;

	// States: normal, crouching, sprinting
	String state = "normal" ;
	bool lowCeiling = false ; // This is for when the ceiling is too low and the player needs to crouch.
	bool wasOnFloor = true ;
	Reticle RETICLE ;
	Control USER_INTERFACE ;
	DebugPanel DEBUG_PANEL ;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        base._Ready();
		speed = baseSpeed ;
		USER_INTERFACE = GetNode<Control>("UserInterface") ;
		DEBUG_PANEL = USER_INTERFACE.GetNode<Control>("DebugPanel") as DebugPanel ;

		Input.MouseMode = Input.MouseModeEnum.Captured ;

		// Set the camera rotation to whatever initial_facing_direction is, as long as it's not Vector3.zero
		if (!initial_facing_direction.Equals(Vector3.Zero))
		{
			HEAD.RotationDegrees = initial_facing_direction ;
		}

		if (defaultReticle != null)
		{
			change_reticle(defaultReticle) ;
		}

		HEADBOB_ANIMATION.Play("RESET");
		JUMP_ANIMATION.Play("RESET");
		CROUCH_ANIMATION.Play("RESET");
    }

	void change_reticle(string reticlePath)
	{
		if (RETICLE!=null)
		{
			RETICLE.QueueFree();
		}

		RETICLE = GD.Load<PackedScene>(reticlePath).Instantiate<Reticle>() ;
		RETICLE.character = this ;
		USER_INTERFACE.AddChild(RETICLE) ;
	}

    public override void _PhysicsProcess(double delta)
    {
		currentSpeed = Vector3.Zero.DistanceTo(GetRealVelocity()) ;
		
		DEBUG_PANEL.AddProperty("Speed", $"{currentSpeed:0.000}", 1) ;
		DEBUG_PANEL.AddProperty("Target Speed", $"{speed}", 2) ;
		Vector3 cv = GetRealVelocity() ;
		DEBUG_PANEL.AddProperty("Velocity", $"X: {cv.X:0.000} Y: {cv.Y:0.000} X: {cv.X:0.000}", 3) ;
		
		// Gravity
		//  If the gravity changes during your game, uncomment this code
		// gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
		HandleGravityAndJumping(delta) ;

		Vector2 inputDir = immobile ? Vector2.Zero : Input.GetVector(LEFT, RIGHT, FORWARD, BACKWARD) ;
		HandleMovement(delta, inputDir) ;
		
		lowCeiling = CEILING_DETECTION.IsColliding() ;
		handleState(inputDir != Vector2.Zero);

		if (dynamicFOV) { updateCameraFOV() ; }
		
		if (viewBobbing) { headbobAnimation(inputDir != Vector2.Zero) ; }
		
		if (jumpAnimation)
		{
			if (!wasOnFloor && IsOnFloor()) // Just Landed
			{
				JUMP_ANIMATION.Play((GD.Randi() % 2) == 1 ? "land_left" : "land_right") ;
			}
			wasOnFloor = IsOnFloor() ; //This must always be at the end of physics_process
		}
    }

	void HandleGravityAndJumping(double delta)
	{
		Vector3 currentVelocity = Velocity ;
		if (!IsOnFloor())
		{
			currentVelocity.Y -= (float)(gravity * delta) ;
		}

		else if (jumpingEnabled)
		{
			if (continuousJumping ? Input.IsActionPressed(JUMP) : Input.IsActionJustPressed(JUMP))
			{
				if (IsOnFloor() && !lowCeiling)
				{
					if (JUMP_ANIMATION != null)
					{
						JUMP_ANIMATION.Play("jump") ;
					}
					currentVelocity.Y  += jumpVelocity ;
				}
			}
        }
		Velocity = currentVelocity ;
	}

	void HandleMovement(double delta, Vector2 inputDir)
	{
		Vector2 direction2D  = inputDir.Rotated(-HEAD.Rotation.Y) ;
		Vector3 direction = new Vector3(direction2D.X, 0, direction2D.Y) ;
		direction = direction.Normalized(); // ?
		MoveAndSlide() ;

		if (!inAirMomentum || IsOnFloor())
		{
			Vector3 currentVelocity = Vector3.Zero ;
			currentVelocity.X = motionSmoothing ? Mathf.Lerp(Velocity.X, direction.X * speed, (float)(acceleration * delta)) : direction.X * speed ;
			currentVelocity.Z = motionSmoothing ? Mathf.Lerp(Velocity.Z, direction.Z * speed, (float)(acceleration * delta)) : direction.Z * speed ;
			Velocity = currentVelocity ;
		}
	}

	void handleState(bool moving)
	{
		if (sprintEnabled)
		{
			if (sprintMode == 0)
			{
				if (Input.IsActionPressed(SPRINT) && state != "crouching")
				{
					if (moving)
					{
						if (state != "sprinting")
						{
							enterSprintState() ;				
						}
					}
					else
					{
						if (state == "sprinting")
						{
							enterNormalState() ;
						}
					}
				}
				else if (state == "sprinting")
				{
					enterNormalState() ;
				}
			}
			else if (sprintMode == 1)
			{
				if (moving)
				{
					if (Input.IsActionPressed(SPRINT) && state == "normal")
					{
						enterSprintState() ;
					}
					if (Input.IsActionJustPressed(SPRINT))
					{
						switch(state)
						{
							case "normal":
								enterSprintState();
								break;
							case "sprinting":
							default:
								enterNormalState();
								break ;
						}
					}
				}
				else if (state == "sprinting")
				{
					enterNormalState() ;
				}
			}
		}

		if (crouchEnabled)
		{
			if (crouchMode == 0)
			{
				if (Input.IsActionPressed(CROUCH) && state != "sprinting")
				{
					if (state != "crouching")
					{
						enterCrouchState() ;
					}
				}
				else if (state == "crouching" && !CEILING_DETECTION.IsColliding())
				{
					enterNormalState() ;
				}
			}
			else if (crouchMode == 1)
			{
				if (Input.IsActionJustPressed(CROUCH))
				{
						switch(state)
						{
							case "normal":
								enterCrouchState();
								break;
							case "crouching":
							default:
								if (!CEILING_DETECTION.IsColliding())
								{
									enterNormalState();
								}
								break ;
						}
				}
			}
		}
	}

    private void enterNormalState()
    {
		String previousState = state ;
		if (previousState == "crouching")
		{
			CROUCH_ANIMATION.PlayBackwards("crouch") ;
		}
		state = "normal" ;
		speed = baseSpeed ;
    }

    private void enterSprintState()
    {
		String previousState = state ;
		if (previousState == "crouching")
		{
			CROUCH_ANIMATION.PlayBackwards("crouch") ;
		}
		state = "sprinting" ;
		speed = sprintSpeed ;
    }

    private void enterCrouchState()
    {
		String previousState = state ;
		state = "crouching" ;
		speed = crouchSpeed ;
		CROUCH_ANIMATION.Play("crouch") ;
    }

	void updateCameraFOV()
	{
		CAMERA.Fov = Mathf.Lerp(CAMERA.Fov, state == "sprinting" ? 85 : 75, 0.3f) ;
	}

	void headbobAnimation(bool moving)
	{
		if (moving && IsOnFloor())
		{
			String useHeadbobAnimation = (state == "normal" || state == "crouching") ? "walk" : "sprint" ;
			bool wasPlaying = HEADBOB_ANIMATION.CurrentAnimation == useHeadbobAnimation ;

			HEADBOB_ANIMATION.Play(useHeadbobAnimation, 0.25f) ;
			HEADBOB_ANIMATION.SpeedScale = (currentSpeed / baseSpeed) * 1.75f ;
			if (!wasPlaying) { HEADBOB_ANIMATION.Seek((double)(GD.Randi() % 2)); }
		}
		else
		{
			HEADBOB_ANIMATION.Play("RESET", 0.25) ;
			HEADBOB_ANIMATION.SpeedScale = 1 ;
		}
	}

    public override void _Process(double delta)
    {
		DEBUG_PANEL.AddProperty("FPS", $"{Performance.GetMonitor(Performance.Monitor.TimeFps)}", 0) ;
		DEBUG_PANEL.AddProperty("state", $"{state}" + (!IsOnFloor() ? " in the air" : ""), 4) ;
		if (Input.IsActionJustPressed(PAUSE))
		{
			Input.MouseMode = (Input.MouseMode == Input.MouseModeEnum.Captured) ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured ;
		}
    }

    public override void _UnhandledInput(InputEvent @event)
    {
		if (@event is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			InputEventMouseMotion iemm = (InputEventMouseMotion)@event ;
			Vector3 currentRotation = HEAD.Rotation ;
			currentRotation.Y -= iemm.Relative.X * mouseSensitivity ;
			currentRotation.X -= iemm.Relative.Y * mouseSensitivity ;
			HEAD.Rotation = currentRotation ;
		}
    }
}
