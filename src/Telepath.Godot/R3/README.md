# R3.Godot runtime subset

Vendored from [Cysharp/R3](https://github.com/Cysharp/R3) (`src/R3.Godot/addons/R3.Godot`), MIT License.

Includes Frame/Time providers, Node/UI/Signal/Delta extensions. Does **not** include ObservableTracker or the official `GodotR3Plugin`.

Do **not** enable the official `addons/R3.Godot` plugin alongside Telepath — type names collide (`namespace R3`).

`FrameProviderDispatcher.gd` is the Autoload shell (also registered by the GDScript editor plugin). It instantiates `FrameProviderDispatcher.cs` only at runtime. The Binding Dock is a GDScript `EditorDock`; `BindingDockBridge` is the only C# `[Tool]` and must not subscribe to Godot signals. Godot resolves C# scripts only from the host assembly.
