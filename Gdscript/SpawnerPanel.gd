extends Node3D

class_name SpawnerPanel

# Simple spawner helper for spawning CSG objects in front of a player/camera.
# This intentionally avoids a complex 3D UI implementation so it's robust across VR/desktop.
# Usage:
#  - Call spawn_box_from_transform(player.global_transform) to create a CSGBox3D in front of the player.
#  - Use set_size(delta) to change the default box size (can wire to mouse wheel/keys).

@export var default_distance: float = 2.0
@export var default_height_offset: float = 0.0
@export var default_box_size: Vector3 = Vector3.ONE * 0.5

var current_size: Vector3

func _ready() -> void:
	current_size = default_box_size

# Spawns a CSGBox3D in front of the given global transform (usually player's camera transform).
# Returns the created node.
func spawn_box_from_transform(origin_transform: Transform3D, distance: float, height_offset: float) -> CSGBox3D:
	if distance == -1.0:
		distance = default_distance
	if height_offset == null:
		height_offset = default_height_offset
	if distance == null:
		distance = default_distance
	var box := CSGBox3D.new()
	box.size = current_size
	# Place it in front of origin
	var t := transform_in_front(origin_transform, distance, height_offset)
	box.global_transform = t
	# A small default material so it's visible in most scenes
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.8, 0.6, 0.3)
	box.material = mat
	add_child(box)
	return box

# Utility to compute a transform in front of an origin transform (camera/player). We'll align rotation to the origin's basis.
func transform_in_front(origin_transform: Transform3D, distance: float, height_offset: float) -> Transform3D:
	# In Godot 4, -Z is forward for cameras
	var forward = -origin_transform.basis.z
	forward = forward.normalized()
	var pos = origin_transform.origin + forward * distance + Vector3(0, height_offset, 0)
	# Keep orientation same as origin (so objects face the same way)
	var basis = origin_transform.basis
	return Transform3D(basis, pos)

# Convenience methods for input-driven resizing
func set_size_delta(delta: float) -> void:
	current_size += Vector3.ONE * delta
	# clamp small values
	current_size.x = max(0.05, current_size.x)
	current_size.y = max(0.05, current_size.y)
	current_size.z = max(0.05, current_size.z)

func set_size_vec(new_size: Vector3) -> void:
	current_size = Vector3(max(0.05, new_size.x), max(0.05, new_size.y), max(0.05, new_size.z))

# Example helper: spawn an instance of an arbitrary PackedScene in front of the player
func spawn_scene_from_transform(packed: PackedScene, origin_transform: Transform3D, distance, height_offset) -> Node:
	var inst = packed.instantiate()
	if inst is Node3D:
		var t = transform_in_front(origin_transform,   distance if distance != null else default_distance, height_offset)
		inst.global_transform = t
		add_child(inst)
		return inst
	else:
		# fallback: add as plain node
		add_child(inst)
		return inst

## Keyboard helpers you can call from a toggle script
func spawn_box_from_nodepath(player_nodepath: NodePath) -> void:
	var p = get_node_or_null(player_nodepath)
	if p and p is Node3D:
		spawn_box_from_transform(p.global_transform, 1, 0)

func debug_print_size() -> void:
	print("Spawner current_size: ", current_size)
