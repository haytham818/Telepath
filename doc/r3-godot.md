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
| `TelepathEditorPlugin.gd` | 编辑器插件外壳（非 C#）；C# 重建时自身不占用 ALC |
| `TelepathBindingDock` | 唯一的 C# `[Tool]`；`ISerializationListener` 在卸载前断开 `EditorSelection` / 绑定并停帧泵 |

`TelepathEditorPlugin` 会 `AddAutoloadSingleton` 注册 **GDScript** 外壳；演示宿主的 `project.godot` 也写了 `[autoload]`，运行时不必先开编辑器。插件入口本身也是 GDScript：C# `EditorPlugin` 会在热重载时被引擎一直握着，从而钉住 ALC。

C# 程序集热重载会失败，如果还有 C# `[Tool]` 把 `Delegate::Invoke` 挂在编辑器单例上。`selection_changed` 改由 GDScript 插件转发；Dock 在 `OnAfterDeserialize` 里丢掉热重载残留的失效 Callable，再重新接线。若仍报错，重启编辑器。

Godot 只在**宿主主程序集**里解析 C# 脚本。`FrameProviderDispatcher.gd` / `.cs` 与 `plugin.cfg` 在 `src/Telepath.Godot/Addon/`，`TelepathEditorPlugin.gd` 与 Binding Dock 在 `src/Telepath.Godot/Editor/`，均符号链接到 `samples/Showcase/addons/Telepath/`，由 `Telepath.Showcase` 编译。`Telepath.Godot` 用 `InternalsVisibleTo("Telepath.Showcase")` 暴露 Provider 的 internal 成员。Addon / Editor 源码不编进 `Telepath.Godot.dll`。

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
