extends Node

class_name PlayerSpawner

# Simple helper to instantiate the existing Gdscript/player.tscn and keep a reusable instance.
# Usage:
#  - Add this node under a persistent scene (HomeScreen/MainGame) or autoload it.
#  - Call spawn_player(parent) to instantiate (reuses existing instance if present).
#  - Call spawn_player_at_transform(transform) to ensure it's placed at a given transform.
#  - Call free_player() to free the instance.

@export var player_scene: PackedScene = preload("res://Gdscript/player.tscn")

var player_instance: Node = null

func spawn_player(parent: Node = null) -> Node:
	if player_instance and is_instance_valid(player_instance):
		return player_instance

	var inst = player_scene.instantiate()
	if parent == null:
		get_tree().get_root().add_child(inst)
	else:
		parent.add_child(inst)

	player_instance = inst
	return inst

func spawn_player_at_transform(t: Transform3D, parent: Node = null) -> Node:
	var inst = spawn_player(parent)
	if inst is Node3D:
		inst.global_transform = t
	return inst

func get_player() -> Node:
	return player_instance

func free_player() -> void:
	if player_instance and is_instance_valid(player_instance):
		player_instance.queue_free()
	player_instance = null

