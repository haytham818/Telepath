# Telepath

基于 [R3](https://github.com/Cysharp/R3) 的 MVVM UI 框架，面向 Godot 4.4+ / .NET 8

## Feature

- **平台无关的 Core**：`Telepath.Core` 不依赖 Godot，同一套 ViewModel 可复用到其他 R3 宿主
- **声明式绑定**：绑定信息可存在于场景节点的.tscn文件中，由编辑器扩展可视化编辑，无需重新编译即可对绑定进行调整
- **源生成器**：使用源生成器减少了大量样板代码
- **内嵌 R3 胶水**：帧/时间时钟、Godot 信号转 Observable，无需再装官方 `R3.Godot` addon

## Requirements

- Godot 4.4+（.NET）
- .NET 8 SDK

## Installation

### NuGet

在宿主工程（Godot.NET.Sdk）里加：

```xml
<PackageReference Include="Telepath.Godot" Version="0.45.2" />
```

或：

```
dotnet add package Telepath.Godot --version 0.45.2
```

`Telepath.Godot` 会带上 `Telepath.Core` 以及源生成器。

然后按下方 **Addon** 一节把编辑器插件拷进工程。

### From Source

1. Clone 或 submodule 本仓库
2. 在宿主工程（Godot.NET.Sdk）里加两个 ProjectReference：

```xml
<ProjectReference Include="path/to/Telepath/src/Telepath.Godot/Telepath.Godot.csproj" />
<ProjectReference Include="path/to/Telepath/src/Telepath.SourceGenerator/Telepath.SourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

`Telepath.Godot` 会附带 `Telepath.Core`

3. 按下方 **Addon** 一节把编辑器插件拷进工程。

### Addon

无论 NuGet 还是源码引用，都需要把 addon 复制（或符号链接）进宿主工程：

| 来源 | 目标 |
|---|---|
| `src/Telepath.Godot/Addon/`（`plugin.cfg`、`FrameProviderDispatcher.gd` / `.cs`） | `res://addons/Telepath/` |
| `src/Telepath.Godot/Editor/`（编辑器插件与 Binding Dock） | `res://addons/Telepath/Editor/` |

构建 Godot 项目后然后在 项目设置 → 插件 里启用

插件会把 GDScript 外壳 `FrameProviderDispatcher.gd` 注册成 Autoload，运行时驱动 R3 时钟；导出的工程同样依赖这个 Autoload，请确认 `project.godot` 里有 `[autoload]` 条目

注意：

- Windows 上 git 符号链接需要 `core.symlinks = true`，否则直接复制
- 不要装官方 `R3.Godot` addon：运行时子集已内嵌（`namespace R3`），同名类型会冲突
- addon 里的 GDScript 外壳与 Dock 由**宿主主程序集**编译，不会进入 `Telepath.Godot.dll`
- 因此 addon 脚本用到的库类型必须是 public；不要用 `InternalsVisibleTo("Telepath.Showcase")` 掩盖真实宿主的可见性问题

## Quick Start

一个最简单的计数器：ViewModel → View → 场景绑定

**1. ViewModel** —— 只写字段与方法，生成器产出 R3 属性与命令：

```csharp
using R3;
using Telepath.Core;

public sealed partial class CounterViewModel : ViewModel
{
    [Bindable]
    private int _count;

    [Command(CanExecute = nameof(CanIncrement))]
    private void OnIncrement() => Count.Value++;

    private Observable<bool> CanIncrement() => Count.Select(c => c < 10);
}
```

`[Bindable] private int _count` 生成 `BindableReactiveProperty<int> Count`；`[Command] OnIncrement()` 生成 `ReactiveCommand IncrementCommand`（`CanExecute` 关联按钮 `Disabled`），两者都登记进 VM 的 `DisposableBag`

**2. View** —— 轻量化对象，标记与通知转发：

```csharp
using Godot;
using Telepath.Godot;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
```

**3. 场景** —— 一个 `Label`（`%CountLabel`）和一个 `Button`（`%IncrementButton`），使用UI绑定侧栏(在Inspector旁边)，调整绑定关系

场景文件中将会附带这样的 `metadata/telepath_bindings`

```tscn
metadata/telepath_bindings = [{
"kind": "Text",
"member": "Count",
"path": "%CountLabel"
}, {
"kind": "Command",
"member": "IncrementCommand",
"path": "%IncrementButton"
}]
```
运行时，编辑器扩展会解析这些绑定信息，并自动生成对应的代码

## Current Supported Bindings

| 控件 | 生成 | 方向 |
|---|---|---|
| `Label` / `RichTextLabel` | `Bind(..., .Text())` | 单向 |
| `LineEdit` / `TextEdit` | `Bind(..., .Text())` | `BindableReactiveProperty<string>` 双向，否则单向 |
| `CheckBox` / `CheckButton` | `Bind(..., .Toggle())` | 双向 `bool` |
| 其他 `BaseButton` | `BindCommand` | ... |
| `Range`（Slider / SpinBox / 进度条） | `Bind(..., .Value())` | `BindableReactiveProperty<double>` 双向 |
| `ItemList` | `BindItems(..., .Items())` | 集合绑定 |
| `OptionButton` | `Bind(..., .Selected())` | 双向 `long` |
| 带 `[TelepathView]` 的 `Control` | `BindView(..., .View())` | ... |
| 普通 `Control` | 报错 | 必须显式指定绑定类型  |

## Directory Structure

```
src/Telepath.Core/             
src/Telepath.Godot/            Godot 层
  Addon/                       
  Editor/                      GDScript 插件
src/Telepath.SourceGenerator/  Roslyn 生成器
samples/Showcase/             演示 Godot 工程（addons/Telepath 为符号链接）
tests/                        单元测试
```

## Showcase

如果你想深入了解，请使用 Godot 打开 `samples/Showcase`

包含详尽的示例与注释帮你理解这个框架

## 依赖注入

Telepath 不提供 DI 容器。

页面 ViewModel 由 `Conductor` / `OverlayHost` 持有：进栈接管、出栈 `Dispose`。若容器再管理同一实例，会双重释放或泄漏。Core 也不绑定某一套容器，宿主自己选。

主线 Showcase 用构造函数传入 `INavigator` / `IOverlayHost` / `IInteraction`，导航时手写 `Navigate(new FooViewModel(...))`。

若要按类型导航，实现 `IViewModelActivator` 并赋给 Conductor 与 Overlay 的 `ViewModelActivator`，即可 `Navigate<T>()` / `Push<T>()`。激活器只负责创建，所有权仍归 Telepath。

接入现成容器的示例（实验分支，不进主线）：

| 分支 | 容器 |
|---|---|
| [`experiment/showcase-msdi`](https://github.com/haytham818/Telepath/tree/experiment/showcase-msdi) | `Microsoft.Extensions.DependencyInjection` |
| [`experiment/showcase-qframework`](https://github.com/haytham818/Telepath/tree/experiment/showcase-qframework) | [QFramework](https://github.com/liangxiegame/qframework) |

## Build & Test

```
dotnet build Telepath.sln
dotnet test  Telepath.sln
```

## License

MIT

See [LICENSE](LICENSE)
