# R3.Godot runtime subset

Vendored from [Cysharp/R3](https://github.com/Cysharp/R3) (`src/R3.Godot/addons/R3.Godot`), MIT License.

Includes Frame/Time providers, Node/UI/Signal/Delta extensions. Does **not** include ObservableTracker or the official `GodotR3Plugin`.

Do **not** enable the official `addons/R3.Godot` plugin alongside Telepath — type names collide (`namespace R3`).

`FrameProviderDispatcher` is a Godot Node script in `src/Telepath.Godot/Addon/`, symlinked into the host addon (`samples/Showcase/addons/Telepath/`) and registered as an Autoload by `TelepathEditorPlugin`. Godot resolves C# scripts only from the host assembly.
