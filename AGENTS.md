# AGENTS.md

面向 Coding Agent 的仓库说明

## 项目概览

Telepath 是一个基于 R3 的 MVVM Godot UI 框架（起步阶段）。

- 引擎: Godot 4.x
- 语言: C# / .NET 8
- 根命名空间: `Telepath`（Core）；Godot 层为 `Telepath.Godot`

## 分层与依赖方向

```
Telepath (宿主) → Telepath.Godot → Telepath.Core → R3
                ↘ Telepath.SourceGenerator (Analyzer only)
```

| 项目 | 路径 | 职责 |
|------|------|------|
| `Telepath.Core` | `addons/Telepath/Core/` | 平台无关：ViewModel、命令、绑定抽象、转换器接口 |
| `Telepath.Godot` | `addons/Telepath/Godot/` | Godot 交互：节点适配、生命周期、EditorPlugin（`Microsoft.NET.Sdk` + GodotSharp，避免库项目嵌套 `.godot`） |
| `Telepath.SourceGenerator` | `addons/Telepath/SourceGenerator/` | Roslyn 代码生成（当前为空骨架） |
| `Telepath`（根） | 仓库根 | Godot 宿主 / 演示，引用 Godot 层与 Analyzer |

### 硬规则

- **Core 禁止** 引用 `Godot` / `GodotSharp`，也禁止 `GODOT` 条件编译
- Godot 可以依赖 Core；反向禁止
- SourceGenerator **不** 引用 Core / Godot（按属性元数据名识别）
- 编辑器插件属于 Godot 层（`#if TOOLS`），不单独开程序集

### R3.Godot

官方 Godot FrameProvider 以 **addon** 形式分发（非 NuGet）。需要时将 `R3.Godot` 插件放入工程并启用；Core 只引用 NuGet 包 `R3`。

### 构建产物

`Directory.Build.props` 将 `bin/` / `obj/` 重定向到仓库外的 `../.telepath-build/`，避免污染 `res://`。

## ViewModel（Core）

公开类型：`IViewModel`、`ViewModel`（`addons/Telepath/Core/ViewModel/`）。

- **属性 / 命令**：直接暴露 R3 的 `BindableReactiveProperty<T>` 与 `ReactiveCommand` / `ReactiveCommand<T>`，不另包一层。
- **生命周期**：仅 `IDisposable`。`Track(disposable)` 登记到内部 `DisposableBag`；`Dispose` 幂等，先 `OnDispose()` 再释放 bag。已 Dispose 后再 `Track` 抛 `ObjectDisposedException`。
- **不实现 INPC**：通知在属性对象上；ViewModel 自身不做属性变更广播。
- **构造期**：不要用帧算子（`IntervalFrame` 等）；需要时在 Godot 层 `ObserveOn`。

### 与 Godot 节点寿命的对应（约定，绑定层尚未实现）

| 节点时机 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` / `_EnterTree` | 仅首次创建时 new；不要每次进树都 new | 接绑定 |
| `_ExitTree` | **不** `Dispose`（节点可能再进树） | 断绑定 |
| 真正释放（`NotificationPredelete` / `Free`） | `viewModel.Dispose()` | 已断 |

用户侧示例：

```csharp
public sealed class CounterViewModel : ViewModel
{
    public BindableReactiveProperty<int> Count { get; }
    public ReactiveCommand Increment { get; }

    public CounterViewModel()
    {
        Count = Track(new BindableReactiveProperty<int>(0));
        Increment = Track(new ReactiveCommand(_ => Count.Value++));
    }
}
```
