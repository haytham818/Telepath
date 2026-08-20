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


```bash
dotnet add package Telepath.Godot
```

`Telepath.Godot` 会带上 `Telepath.Core` 以及源生成器。

然后按下方 **Addon** 一节把编辑器插件接入工程。

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
| `src/Telepath.Godot/Addon/`（含 `Editor/` Binding Dock） | `res://addons/Telepath/` |

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

## 导航模型

Telepath 采用 **ViewModel优先的状态驱动导航**，不提供 URL / Route 路由器

页面状态存在于 ViewModel 层，Godot View 只负责把当前状态投影到场景

```text
ShellViewModel
├─ Conductor
│  ├─ BackStack: [Page A, Page B]
│  └─ ActiveItem: Page C
└─ OverlayHost
   ├─ Popup: [Panel]
   ├─ Modal: [Dialog]
   └─ Toast: [Toast]
```

### 主页面管理：`Conductor`

`Conductor` 是一个单内容槽的回退栈：

- `ActiveItem` 是当前页面，View 只需观察它
- `Navigate(viewModel)` 停用当前页面并压入回退栈，再激活新页面
- `Back()` 销毁离开的页面 ViewModel，并恢复上一页
- `Close(viewModel)` 可以关闭当前页或回退栈中的指定页面
- `CanGoBack` 和 `BackCommand` 可直接用于 UI 绑定

```text
初始：         Active=A, Stack=[]
Navigate(B)：  Active=B, Stack=[A]
Navigate(C)：  Active=C, Stack=[A, B]
Back()：       Active=B, Stack=[A]     // C Dispose
```

子页面通常通过构造函数被注入更小的 `INavigator` 接口，只发出导航请求，不依赖具体宿主：

```csharp
public sealed class DirectoryViewModel(INavigator navigator) : ViewModel
{
    private void OnOpenCounter() =>
        navigator.Navigate(new CounterViewModel());
}
```

`Conductor` 保留回退栈中的 **ViewModel**，但不缓存 Godot **View**。`ActiveItem` 变化时，`ContentPresenter` 会释放旧 View；返回时再根据同一个 ViewModel 创建新 View。因此需要跨页面保留的状态应放在 ViewModel 或独立领域模型中，不要依赖 Godot 节点实例一直存活。

### 覆盖层宿主：`OverlayHost`

覆盖层不进入主页面回退栈。`OverlayHost` 管理多个具名 Band，每个 Band 都是独立的栈；`Order` 决定 Band 之间的视觉层级。

| 内建 Band | Order | 处理被覆盖的ViewModel | 默认 Cover 模式 | 阻断下层输入 |
|---|---:|---|---|---|
| `Popup` | 0 | 是 | `Pause` | 是 |
| `Modal` | 100 | 是 | `Pause` | 是 |
| `Toast` | 200 | 否 | `Continue` | 否 |

- `CoverMode.Pause` 对被覆盖项调用 `IActivatable.Deactivate()`，关闭后重新激活
- `CoverMode.Continue` 让被覆盖项继续运行
- 被覆盖的页面和 Overlay View 会保留在场景树中并继续绑定，只有出栈项会被释放
- `BlocksPassThrough` 控制 GUI 点击与焦点隔离，与 `CoverMode` 是两个独立概念

`Pause` 表示 ViewModel 展示生命周期的停用，**不会自动设置 `SceneTree.Paused`，也不会停止 `Node._Process()` / `_PhysicsProcess()`**。游戏宿主需在 `IActivatable` 或自己的会话服务中显式暂停玩法和输入。

### 组合返回策略

返回优先级由 Shell 决定，而不是框架全局硬编码。常见策略是先关闭最高的可回退 Overlay，再返回主页面：

```csharp
public sealed class ShellViewModel : Conductor
{
    public OverlayHost Overlay { get; }

    public ShellViewModel()
    {
        Overlay = Track(new OverlayHost(() => ActiveItem.Value));
        Track(Overlay.HasBackableOverlay.Subscribe(_ => UpdateCanGoBack()));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        // 避免“清 Overlay → 旧页面短暂激活 → 立即换页”。
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack() =>
        Overlay.HasBackableOverlay.Value || base.ComputeCanGoBack();
}
```

`Navigate(viewModel)` / `Overlay.Push(viewModel)` 都会将实例所有权交给对应容器，页面出栈或容器销毁时由 Telepath `Dispose`。不要再让 DI 容器或其他所有者管理同一个页面实例。

## 推荐的 UI 节点树

Shell 是应用的稳定宿主；`Content` 是主页面的单槽目标，`Overlay` 是所有覆盖层的根节点。两者建议是全屏兄弟 `Control`，并让 `Overlay` 处于更高的绘制顺序。

```text
ShellView : Control                  [TelepathView<ShellViewModel>]
├─ Content : Control                %Content
│  └─ <当前 PageView>           由 ContentPresenter 动态放入
└─ Overlay : Control                %Overlay
   ├─ Popup : Control              由 OverlayHostPresenter 动态创建
   ├─ Modal : Control
   └─ Toast : Control
```

`Content` 和 `Overlay` 必须共享同一个 `PresentedViews`，才能让 Overlay 对当前页面正确播放覆盖动画、隔离焦点并在关闭后恢复焦点。

### 承载 `SubViewportContainer` 游戏内容

默认 `ViewRegistry` 把注册场景实例化为 `Control`。以 `SubViewportContainer` 承载 2D / 3D 世界时，可以把整个游戏会话作为一个 Gameplay View：

```text
ShellView : Control
├─ Content : Control
│  └─ GameplayView : Control          [TelepathView<GameplayViewModel>]
│     └─ AspectRatioContainer
│        └─ GameViewportContainer : SubViewportContainer
│           └─ GameViewport : SubViewport
│              └─ World : Node2D / Node3D
└─ Overlay : Control
   ├─ Pause
   ├─ Inventory
   ├─ Settings
   └─ Toast
```

推荐的职责边界：

- `Conductor` 管理“是否处于游戏会话”，例如 `MainMenu → Gameplay → Result`
- Gameplay View 内部的世界宿主管理玩家、关卡和场景加载
- 暂停、背包、设置和确认框使用 Overlay，使 Gameplay View 继续存在
- 离开 Gameplay 页面会释放整棵 View 子树，包括 `SubViewport` 内的世界节点；只有在结束或卸载游戏会话时才应该这样导航
- 如果需要返回 Gameplay 后恢复世界，将状态放在 ViewModel / GameSession / 存档快照中，不要只放在 Godot 节点字段中

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

文件夹只用来分职责，**不等于命名空间**（库代码仍是 `Telepath.Core` / `Telepath.Godot`）。

```
src/Telepath.Core/             平台无关
  ViewModel/
  Binding/                     Attributes / Collection / Converters
  Presentation/                Activation / Conductor / Overlay / Interaction / NestedView
src/Telepath.Godot/            Godot 层
  View/                        资源寿命
  Binding/                     Attributes / Scene / Targets / Collection
  Presentation/                Hosting / Conductor / Overlay / NestedView / Transition / Focus
  Addon/                       完整插件树（排除出库编译）
    Editor/                    Binding Dock 与编辑器插件
  R3/                          内嵌 R3 Godot 胶水
src/Telepath.SourceGenerator/  Roslyn 生成器
samples/Showcase/              演示 Godot 工程（addons/Telepath → Addon/）
tests/                         单元测试
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
