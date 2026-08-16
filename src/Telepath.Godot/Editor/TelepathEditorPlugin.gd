@tool
extends EditorPlugin

## GDScript shell so the plugin instance itself is not a C# [Tool] script.
## Godot holds EditorPlugin alive across C# rebuilds; a C# plugin pins the ALC
## (godotengine/godot#78513). The dock is still C# and implements
## ISerializationListener to drop EditorSelection / R3 subscriptions first.

const AutoloadName := "FrameProviderDispatcher"
const AutoloadPath := "res://addons/Telepath/FrameProviderDispatcher.gd"
const DockScenePath := "res://addons/Telepath/Editor/TelepathBindingDock.tscn"

var _dock: EditorDock

func _enter_tree() -> void:
	remove_autoload_singleton(AutoloadName)
	add_autoload_singleton(AutoloadName, AutoloadPath)
	var packed := load(DockScenePath) as PackedScene
	if packed == null:
		push_error("Telepath: missing TelepathBindingDock.tscn")
		return
	_dock = packed.instantiate() as EditorDock
	if _dock.has_method("Attach"):
		_dock.call("Attach", self)
	add_dock(_dock)

func _exit_tree() -> void:
	if is_instance_valid(_dock):
		if _dock.has_method("Detach"):
			_dock.call("Detach")
		remove_dock(_dock)
		_dock.free()
	_dock = null
	remove_autoload_singleton(AutoloadName)
