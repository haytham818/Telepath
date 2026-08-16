@tool
extends EditorPlugin

## GDScript shell so the plugin instance is not a C# [Tool] script.
## selection_changed and dock button signals stay in GDScript; C# ManagedCallables
## on editor objects pin the ALC across rebuilds (godotengine/godot#81903).

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
	var selection := get_editor_interface().get_selection()
	if not selection.selection_changed.is_connected(_on_selection_changed):
		selection.selection_changed.connect(_on_selection_changed)

func _exit_tree() -> void:
	var selection := get_editor_interface().get_selection()
	if selection.selection_changed.is_connected(_on_selection_changed):
		selection.selection_changed.disconnect(_on_selection_changed)
	if is_instance_valid(_dock):
		if _dock.has_method("Detach"):
			_dock.call("Detach")
		remove_dock(_dock)
		_dock.free()
	_dock = null
	remove_autoload_singleton(AutoloadName)

func _on_selection_changed() -> void:
	if is_instance_valid(_dock) and _dock.has_method("NotifySelectionChanged"):
		_dock.call("NotifySelectionChanged")
