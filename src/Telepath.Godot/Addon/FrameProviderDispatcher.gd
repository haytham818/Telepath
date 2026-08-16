extends Node

## GDScript Autoload shell. A C# Autoload script stays alive in the editor and
## pins Godot's collectible AssemblyLoadContext across rebuilds
## (godotengine/godot#78513, Cysharp/R3#125). Runtime instantiates the C# pump
## as a child; the editor plugin owns the editor-side pump instead.

var _runner: Node

func _ready() -> void:
	if Engine.is_editor_hint():
		return
	_attach_runner()

func _exit_tree() -> void:
	_free_runner()

func _attach_runner() -> void:
	var script := load("res://addons/Telepath/FrameProviderDispatcher.cs") as Script
	if script == null:
		push_error("Telepath: missing FrameProviderDispatcher.cs")
		return
	_runner = script.new() as Node
	_runner.name = "Runner"
	add_child(_runner)

func _free_runner() -> void:
	if _runner != null and is_instance_valid(_runner):
		_runner.free()
	_runner = null
