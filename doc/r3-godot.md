# R3.Godot 运行时胶水

路径：`addons/Telepath/Godot/R3/`（`namespace R3`）。

从 [Cysharp/R3](https://github.com/Cysharp/R3) 的 `R3.Godot` addon 搬入的运行时子集（MIT）。**不要**再启用官方 `addons/R3.Godot`——同名类型会冲突。

不含 ObservableTracker（订阅泄漏调试窗）。

## 时钟

| 类型 | 作用 |
|------|------|
| `GodotFrameProvider.Process` / `PhysicsProcess` | 帧时钟 |
| `GodotTimeProvider.Process` / `PhysicsProcess` | 时间时钟 |
| `GodotProviderInitializer` | 设置 `ObservableSystem` 默认 Provider，未处理异常 `GD.PrintErr` |
| `FrameProviderDispatcher` | Autoload；每帧 `Run`，并在 `_Ready` 里初始化默认系统 |

`TelepathEditorPlugin` 会 `AddAutoloadSingleton`；演示宿主的 `project.godot` 也写了 `[autoload]`，运行时不必先开编辑器。

Godot 目前只在**宿主主程序集**里解析 C# 脚本。因此 `FrameProviderDispatcher` 与 `TelepathEditorPlugin` 虽放在 `addons/Telepath/Godot/`，但由宿主 `Telepath.csproj` 编译（`Telepath.Godot` 排除这两文件，并用 `InternalsVisibleTo` 暴露 Provider 的 internal 成员）。

`Delay` / `Throttle` / `Timeout` / `ObserveOn` / `IntervalFrame` 等依赖默认 Provider。ViewModel **构造期**不要用帧算子；Autoload `_Ready` 之后才有默认时钟。

## 胶水 API

| 文件 | API |
|------|-----|
| `GodotNodeExtensions` | `AddTo(Node)` — 节点出树时 Dispose |
| `GodotUINodeExtensions` | `SubscribeToLabel`、`OnPressedAsObservable`、`OnToggledAsObservable`、`OnValueChangedAsObservable`、`OnTextChangedAsObservable`、`OnTextSubmittedAsObservable`、`OnItemSelectedAsObservable` |
| `GodotSignalMapper` | `SignalAsObservable`、`CancelOnSignal`（Node 重载在 `TreeExited` 时完成） |
| `GodotObservableExtensions` | `Delta`（依赖 `GodotFrameProvider`） |

这些是 R3 胶水，供 View 层使用；本仓库尚未包成 Telepath 声明式绑定。

## `AddTo(Node)` 与 ViewModel 寿命

`AddTo(Node)` 在 `TreeExited` 时 Dispose。适合**绑定订阅**（出树断绑定），**不适合 ViewModel**（出树不断 VM，真正释放才 `Dispose`）。

```csharp
// OK：订阅随节点出树释放
button.OnPressedAsObservable().Subscribe(_ => { }).AddTo(this);

// 不要：会提前 Dispose ViewModel
// viewModel.AddTo(this);
```

详见 [viewmodel.md](viewmodel.md)。
