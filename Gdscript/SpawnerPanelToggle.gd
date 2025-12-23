extends Node

# Spawns or toggles a single SpawnerPanel Node3D instance. Works for both VR and desktop players.
# Add an InputMap action named "spawn_panel_toggle" and bind it to a key (e.g., P).
# If `panel_scene` is set to a scene that contains `SpawnerPanel.gd` as its root script or has that script
# as a child, the toggle will create or reuse an instance. If `panel_scene` is empty, it will create a
# `SpawnerPanel` node directly as a child.

@export var panel_scene: PackedScene
@export var spawn_distance: float = 2.0
@export var player_node_path: NodePath

var panel_instance: Node3D
var panel_open: bool = false
var _internal_spawner: Node = null

func _ready() -> void:
	pass

func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if Input.is_action_pressed("spawn_panel"):
			toggle_panel()

func _unhandled_input(event: InputEvent) -> void:
	# Desktop interactions: when the panel is open, use mouse wheel to resize and left click to spawn.
	if not panel_open:
		return
	# Mouse wheel (BUTTON_WHEEL_UP=4, BUTTON_WHEEL_DOWN=5)
	if event is InputEventMouseButton:
		if event.button_index == 4 and event.pressed:
			# wheel up -> increase
			if _internal_spawner and _internal_spawner.has_method("set_size_delta"):
				_internal_spawner.set_size_delta(0.05)
			return
		elif event.button_index == 5 and event.pressed:
			# wheel down -> decrease
			if _internal_spawner and _internal_spawner.has_method("set_size_delta"):
				_internal_spawner.set_size_delta(-0.05)
			return
		# Left click -> spawn box
		if event.button_index == 1 and event.pressed:
			spawn_box()

func toggle_panel() -> void:
	if panel_open:
		close_panel()
	else:
		open_panel()

func open_panel() -> void:
	if not panel_instance:
		if panel_scene:
			panel_instance = panel_scene.instantiate() as Node3D
		else:
			panel_instance = Node3D.new()
			# Attach our spawner helper as a child so spawn logic is available
			var spawner_script = load("res://Gdscript/SpawnerPanel.gd")
			if spawner_script:
				var spawner = spawner_script.new() as Node
				spawner.name = "SpawnerPanel"
				panel_instance.name = "SpawnerPanelRoot"
				panel_instance.add_child(spawner)
		add_child(panel_instance)
	# Position in front of player if available
	if player_node_path != NodePath():
		var p = get_node_or_null(player_node_path)
		if p and p is Node3D:
			var transform_source = _get_best_spawn_transform(p)
			# Position the panel instance in front of the player/camera
			var forward = -transform_source.basis.z.normalized()
			panel_instance.global_transform = Transform3D(transform_source.basis, transform_source.origin + forward * spawn_distance)
	# Try to cache an internal spawner reference for quick calls
	_internal_spawner = panel_instance.get_node_or_null("SpawnerPanel")
	if not _internal_spawner:
		_internal_spawner = panel_instance.get_node_or_null("SpawnerPanelRoot/SpawnerPanel")
	# Show
	panel_instance.show()
	panel_open = true

func close_panel() -> void:
	if panel_instance:
		panel_instance.hide()
	panel_open = false

# Prefer using an XRCamera3D/Camera3D child of the player for spawn positioning, fallback to player's transform
func _get_best_spawn_transform(player_node: Node) -> Transform3D:
	# Search for XRCamera3D first
	var cam = player_node.get_node_or_null("XROrigin3D/XRCamera3D")
	if cam and cam is Node3D:
		return cam.global_transform
	# Otherwise search for any Camera3D/XRCamera3D descendant
	for c in player_node.get_children():
		if c is Camera3D or c.get_class() == "XRCamera3D":
			return c.global_transform
		# recurse into subtrees
		if c is Node:
			var found = _find_camera_recursive(c)
			if found:
				return found.global_transform
	# fallback
	if player_node is Node3D:
		return player_node.global_transform
	return Transform3D()

func _find_camera_recursive(n: Node) -> Node:
	for child in n.get_children():
		if child is Camera3D or child.get_class() == "XRCamera3D":
			return child
		if child is Node:
			var f = _find_camera_recursive(child)
			if f:
				return f
	return null

# Call spawn box on the internal spawner script
@rpc
func spawn_box() -> void:
	if not panel_instance:
		open_panel()
	# if we cached the spawner use it
	var spawner = _internal_spawner
	if not spawner:
		spawner = panel_instance.get_node_or_null("SpawnerPanel")
	if not spawner:
		spawner = panel_instance.get_node_or_null("SpawnerPanelRoot/SpawnerPanel")
	if spawner and spawner.has_method("spawn_box_from_transform"):
		var player = get_node_or_null(player_node_path)
		if player and player is Node3D:
			var transform_source = _get_best_spawn_transform(player)
			spawner.spawn_box_from_transform(transform_source)
		else:
			spawner.spawn_box_from_transform(Transform3D())
