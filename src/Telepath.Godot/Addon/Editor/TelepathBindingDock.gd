@tool
extends EditorDock

## GDScript dock so UI signals are not C# ManagedCallables.
## Godot snapshots those before ISerializationListener; lambdas / FromEvent
## become delegate_handle == null and pin the ALC (godotengine/godot#81903).

const BridgeScript := "res://addons/Telepath/Editor/BindingDockBridge.cs"

var _bridge: Object
var _item_scene_dialog: EditorFileDialog


func _ready() -> void:
	_wire()


func Attach(plugin: EditorPlugin) -> void:
	_ensure_bridge()
	_bridge.call("Attach", plugin, self)


func Detach() -> void:
	_close_item_scene_dialog()
	if is_instance_valid(_bridge) and _bridge.has_method("Detach"):
		_bridge.call("Detach")
	if is_instance_valid(_bridge):
		_bridge.free()
	_bridge = null


func NotifySelectionChanged() -> void:
	_call_bridge("NotifySelectionChanged")


func _ensure_bridge() -> void:
	if is_instance_valid(_bridge):
		return
	var script := load(BridgeScript) as Script
	if script == null:
		push_error("Telepath: missing BindingDockBridge.cs")
		return
	_bridge = script.new()


func _call_bridge(method: String, args: Array = []) -> void:
	if is_instance_valid(_bridge) and _bridge.has_method(method):
		_bridge.callv(method, args)


func _wire() -> void:
	_connect_signal(%BeginAdd.pressed, _on_begin_add)
	_connect_signal(%Add.pressed, _on_add)
	_connect_signal(%Update.pressed, _on_update)
	_connect_signal(%Remove.pressed, _on_remove)
	_connect_signal(%BrowseItemScene.pressed, _on_browse_item_scene)
	_connect_signal(%SceneList.item_selected, _on_scene_selected)
	_connect_signal(%ControlOption.item_selected, _on_control_selected)
	_connect_signal(%MemberOption.item_selected, _on_member_selected)
	_connect_signal(%KindOption.item_selected, _on_kind_selected)
	_connect_signal(%ConverterOption.item_selected, _on_converter_selected)
	_connect_signal(%ParameterOption.item_selected, _on_parameter_selected)
	_connect_signal(%ItemViewOption.item_selected, _on_item_view_selected)
	_connect_signal(%ItemScene.text_changed, _on_item_scene_changed)


func _connect_signal(sig: Signal, cb: Callable) -> void:
	if not sig.is_connected(cb):
		sig.connect(cb)


func _on_begin_add() -> void:
	_call_bridge("BeginAdd")


func _on_add() -> void:
	_call_bridge("Add")


func _on_update() -> void:
	_call_bridge("Update")


func _on_remove() -> void:
	_call_bridge("Remove")


func _on_scene_selected(index: int) -> void:
	_call_bridge("SelectScene", [index])


func _on_control_selected(index: int) -> void:
	_call_bridge("SelectControl", [index])


func _on_member_selected(index: int) -> void:
	_call_bridge("SelectMember", [index])


func _on_kind_selected(index: int) -> void:
	_call_bridge("SelectKind", [index])


func _on_converter_selected(index: int) -> void:
	_call_bridge("SelectConverter", [index])


func _on_parameter_selected(index: int) -> void:
	_call_bridge("SelectParameter", [index])


func _on_item_view_selected(index: int) -> void:
	_call_bridge("SelectItemView", [index])


func _on_item_scene_changed(text: String) -> void:
	_call_bridge("SetItemScene", [text])


func _on_browse_item_scene() -> void:
	_close_item_scene_dialog()
	var dialog := EditorFileDialog.new()
	dialog.file_mode = EditorFileDialog.FILE_MODE_OPEN_FILE
	dialog.access = EditorFileDialog.ACCESS_RESOURCES
	dialog.title = "选择项场景"
	dialog.filters = PackedStringArray(["*.tscn ; Scene"])
	dialog.file_selected.connect(_on_item_scene_picked)
	dialog.canceled.connect(_on_item_scene_canceled)
	_item_scene_dialog = dialog
	add_child(dialog)
	dialog.popup_centered(Vector2i(720, 480))


func _on_item_scene_picked(path: String) -> void:
	_call_bridge("SetItemScene", [path])
	_close_item_scene_dialog()


func _on_item_scene_canceled() -> void:
	_close_item_scene_dialog()


func _close_item_scene_dialog() -> void:
	if not is_instance_valid(_item_scene_dialog):
		_item_scene_dialog = null
		return
	var dialog := _item_scene_dialog
	_item_scene_dialog = null
	if dialog.file_selected.is_connected(_on_item_scene_picked):
		dialog.file_selected.disconnect(_on_item_scene_picked)
	if dialog.canceled.is_connected(_on_item_scene_canceled):
		dialog.canceled.disconnect(_on_item_scene_canceled)
	dialog.queue_free()
