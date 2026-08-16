# R3.Godot 运行时胶水

路径：`src/Telepath.Godot/R3/`（`namespace R3`）。Autoload 外壳是 `src/Telepath.Godot/Addon/FrameProviderDispatcher.gd`，C# 泵是同目录的 `FrameProviderDispatcher.cs`，符号链接到 `samples/Showcase/addons/Telepath/`。

从 [Cysharp/R3](https://github.com/Cysharp/R3) 的 `R3.Godot` addon 搬入的运行时子集（MIT）。**不要**再启用官方 `addons/R3.Godot`——同名类型会冲突。

不含 ObservableTracker（订阅泄漏调试窗）。

## 时钟

| 类型 | 作用 |
|------|------|
| `GodotFrameProvider.Process` / `PhysicsProcess` | 帧时钟 |
| `GodotTimeProvider.Process` / `PhysicsProcess` | 时间时钟 |
| `GodotProviderInitializer` | 设置 `ObservableSystem` 默认 Provider，未处理异常 `GD.PrintErr` |
| `FrameProviderDispatcher.gd` | 宿主 Autoload 外壳；**编辑器里不创建 C# 子节点**，避免热重载钉住 ALC |
| `FrameProviderDispatcher.cs` | 运行时 Autoload 子节点；每帧 `Run`，并在 `_Ready` 里初始化默认系统 |
| `TelepathEditorPlugin` | 编辑器侧自己泵帧（`GodotFramePump`），`_ExitTree` 时停泵并同步释放 Dock |

`TelepathEditorPlugin` 会 `AddAutoloadSingleton` 注册 **GDScript** 外壳；演示宿主的 `project.godot` 也写了 `[autoload]`，运行时不必先开编辑器。

C# 程序集热重载（改代码后重建）会失败，如果编辑器里还活着 C# Autoload / `EditorSelection` 订阅 / 未 `Free` 的 `EditorDock`。这是 Godot [#78513](https://github.com/godotengine/godot/issues/78513) 的已知限制，不是 Telepath 能彻底修掉的引擎 bug。上面的外壳 + 同步拆 Dock 是为了让卸载尽量成功；若仍出现 `Failed to unload assemblies`，重启编辑器。

Godot 只在**宿主主程序集**里解析 C# 脚本。`FrameProviderDispatcher.gd` / `.cs` 与 `plugin.cfg` 在 `src/Telepath.Godot/Addon/`，`TelepathEditorPlugin` 在 `src/Telepath.Godot/Editor/`，均符号链接到 `samples/Showcase/addons/Telepath/`，由 `Telepath.Showcase` 编译。`Telepath.Godot` 用 `InternalsVisibleTo("Telepath.Showcase")` 暴露 Provider 的 internal 成员。Addon / Editor 源码不编进 `Telepath.Godot.dll`。

`Delay` / `Throttle` / `Timeout` / `ObserveOn` / `IntervalFrame` 等依赖默认 Provider。ViewModel **构造期**不要用帧算子；Autoload `_Ready` 之后才有默认时钟。

## 胶水 API

| 文件 | API |
|------|-----|
| `GodotNodeExtensions` | `AddTo(Node)` — 节点出树时 Dispose |
| `GodotUINodeExtensions` | `SubscribeToLabel`、`OnPressedAsObservable`、`OnToggledAsObservable`、`OnValueChangedAsObservable`、`OnTextChangedAsObservable`、`OnTextSubmittedAsObservable`、`OnItemSelectedAsObservable` |
| `GodotSignalMapper` | `SignalAsObservable`、`CancelOnSignal`（Node 重载在 `TreeExited` 时完成） |
| `GodotObservableExtensions` | `Delta`（依赖 `GodotFrameProvider`） |

这些是 R3 胶水。声明式绑定（`Bind` + `.Text()` / `.Toggle()` / `.Value()` / `.Selected()`，以及 `BindCommand`）复用它们，见 [view.md](view.md)。

## `AddTo(Node)` 与 ViewModel 寿命

`AddTo(Node)` 在 `TreeExited` 时 Dispose。适合**绑定订阅**（出树断绑定），**不适合 ViewModel**（出树不断 VM，真正释放才 `Dispose`）。

```csharp
// OK：订阅随节点出树释放
button.OnPressedAsObservable().Subscribe(_ => { }).AddTo(this);

// 不要：会提前 Dispose ViewModel
// viewModel.AddTo(this);
```

详见 [viewmodel.md](viewmodel.md)、[view.md](view.md)。
