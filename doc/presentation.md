# Presentation

导航状态在 ViewModel 上，不是 Autoload 服务定位器。

- **单槽换屏**（`Conductor`）：当前页独占槽；换页默认 `QueueFree` 旧视图，实现了 `IViewTransition` 的页先退场再释放；VM 可留在返回栈。
- **Overlay 栈**（`Overlay`）：一带之内多层视图同时留在树上并保持绑定。`CoverMode.Pause` 会 `Deactivate` 被盖住的页；`CoverMode.Continue` 让它继续跑。
- **命名带**（`OverlayHost`）：多个独立 `Overlay` 栈，带间 z 序固定。Toast 永远在 Modal 之上，不和 Dialog 抢同一个 Back。
- **Interaction**：页 VM `await Run(dialog)`，结果回来再继续。兑现走 `OverlayLayer.Modal`，不是消息总线。
- **BindView**：父场景里已有的子 TelepathView 当绑定目标，注入父 VM 上的子 VM；不实例化、不 `QueueFree`。

```
src/Telepath.Core/Presentation/
  IActivatable.cs                 可选进前台 / 离前台钩子
  Conductor/
    INavigator.cs                 子页请求 Navigate / Back / CloseSelf
    IConductor.cs                 当前页、Close、栈
    Conductor.cs                  单槽：压栈、弹出、离栈即 Dispose
    ContentTarget.cs / ContentBindingExtensions.cs   BindContent
  Overlay/
    IOverlay.cs                   叠层 Push / Back / CloseSelf / Clear
    IOverlayHost.cs               命名带、Register、带间 Back
    OverlayLayer.cs               Popup / Modal / Toast 标识与 z 序
    CoverMode.cs                  Pause（停下层）/ Continue（下层继续跑）
    Overlay.cs                    一带之内：留视图、遮挡暂停
    OverlayHost.cs                多带：独立栈 + 带间 Reconcile 激活 + Covered
    OverlayTarget.cs / OverlayBindingExtensions.cs   BindOverlay
  Interaction/
    IInteraction.cs               Run，请求/应答
    DialogViewModel.cs            对话框 VM：Complete / Dismissed
    Interaction.cs                包装 OverlayHost，默认推进 Modal
  NestedView/
    ViewTarget.cs / ViewBindingExtensions.cs         BindView
src/Telepath.Godot/Presentation/
  ViewRegistry.cs                 ViewModel 类型 → PackedScene
  ViewInjection.cs
  PresentedViews.cs               Content + Overlay 共用的 VM→View 表
  Content/
    ContentPresenter.cs           换页：旧页退场与新页进场并行
    GodotContentTargets.cs        Control.Content(registry, presented)
  Overlay/
    OverlayPresenter.cs           一带：增层进场、删层退场后再 QueueFree
    OverlayHostPresenter.cs       按 Bands 建槽，订阅 Covered 播遮盖
    GodotOverlayTargets.cs        Control.Overlays(registry) / BindOverlayHost
  NestedView/
    GodotViewTargets.cs           Control.View()
  Transition/
    IViewTransition.cs            可选进/退场；View 自播，Presenter 等退场再 QueueFree
    IViewCoverTransition.cs       可选被盖住/揭开；View 仍绑定
    CoverSession.cs               Covered diff 后并行 PlayCover / PlayUncover
    AnimationPlayerTransition.cs  播 AnimationPlayer 并接入取消
    TweenTransition.cs            等 Tween 结束并接入取消
```

示例：[Shell](../samples/Showcase/Shell/)（目录进 Counter / Search / List / Todo / Form + Overlay pause / Modal / Toast / 自定义 Banner；Todo 删除前确认；头上 Status 走 BindView；壳上 Back 先关可返回的 Overlay，跳过 Toast）。进退场示例：Directory↔Counter 淡入淡出（Tween）；Confirm / Toast / Banner 用 Tween；About 用场景里的 `AnimationPlayer`。未实现 `IViewTransition` 的页仍瞬切。

## 两条寿命

资源寿命仍由 [`ViewLifecycle`](../src/Telepath.Godot/View/ViewLifecycle.cs) 驱动，**不要**把打开 / 关闭 / 暂停塞进 Ready / ExitTree / Predelete。

| | 资源寿命 | 呈现寿命 |
|---|---|---|
| 谁驱动 | Godot 节点通知 | 导体 / Overlay / OverlayHost |
| 进 | Ready：创建或拿到 VM，接绑定；View `OnBind`，VM `OnBound` | `IActivatable.Activate`：进前台 |
| 出树 / 被盖住 | ExitTree：**只断绑定**；View `OnUnbind`，VM `OnUnbound` | `Deactivate`：失焦或被遮挡；默认**不断绑定、不 Dispose** |
| 结束 | Predelete：View `OnPredelete`，仅自有 VM `Dispose` | Close / Back：出栈；默认 Dispose 离栈页 |

打开 ≠ `new`，关闭 ≠ `Dispose`，暂停 ≠ `ExitTree`。进场 / 退场动画也不是呈现寿命：见下方 [`IViewTransition`](#iviewtransition)。被盖住页的视觉过渡见 [`IViewCoverTransition`](#iviewcovertransition)。

单槽换页会拆掉旧视图（出树断绑定），VM 仍可在返回栈上；再 `Back` 时重新实例化视图并注入同一 VM。若旧视图实现了 `IViewTransition`，解绑后节点会作为幽灵留一帧或多帧播退场，再 `QueueFree`。Overlay 相反：被盖住的视图留在树上，绑定不断；`CoverMode.Pause` 才 `Deactivate`。关掉的那一层走退场。被盖住的页若实现了 `IViewCoverTransition`，与新层进场**并行**播遮盖。

## 导体

`Conductor` 继承 `ViewModel`，实现 `IConductor` / `INavigator`。

- `Navigate(vm)`：若已有当前页则 `Deactivate` 并压栈，再把 `ActiveItem` 设为 `vm` 并 `Activate`。导体取得所有权。
- `Back()`：弹出；`Deactivate` 并 `Dispose` 当前页，恢复上一页并 `Activate`。栈空时返回 `false`。
- `Close(vm)`：当前页等价于 `Back`（栈空则清空槽）；栈中的页直接移出并 `Dispose`。
- `CloseSelf()`：关闭当前页；无当前页则忽略。
- `CanGoBack` / `BackCommand`：给壳上的返回按钮。`ComputeCanGoBack` 可覆盖（壳把可返回的 Overlay 带算进去）。
- 同一实例已是当前页：`Navigate` 忽略。已在栈上：抛错（不做缓存 / bring-to-front）。
- 导体 `Dispose` 时释放当前页和栈上剩余页。

子页不要认识壳类型。需要跳转时，构造期注入 `INavigator`；需要提问时注入 `IInteraction`（与 Todo 项注入回调相同）。

```csharp
public sealed class ShellViewModel : Conductor
{
    public static OverlayLayer Banner { get; } = new(
        "Banner", order: 50, handlesBack: false,
        defaultCover: CoverMode.Continue, blocksPassThrough: false);

    public OverlayHost Overlay { get; }
    public IInteraction Interaction { get; }

    public ShellViewModel()
    {
        Overlay = Track(new OverlayHost(() => ActiveItem.Value));
        Overlay.Register(Banner);
        Interaction = new Interaction(Overlay);
        Track(Overlay.HasBackableOverlay.Subscribe(_ => UpdateCanGoBack()));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack()
        => Overlay.HasBackableOverlay.Value || base.ComputeCanGoBack();
}
```

壳 View 先 `BindContent` / `BindOverlayHost` 再 `Navigate` 首页，这样第一页的 `Activate` 也发生在视图进树并接绑定之后。出树再进树时 `ActiveItem` 仍在，只重新 `Present`，不再 `Navigate`。

`IActivatable` 是可选的，**不**进 `IViewModel`。需要停轮询、停请求的页再实现。切换时：先 `Deactivate` 旧前台，改栈（宿主同步改视图），再 `Activate` 新前台。它**不是**打开 / 关闭动画。

## Overlay

`Overlay` 不实现 `INavigator`，避免面板 `Back` 误伤屏幕栈。面板拿 `IOverlay`。一带之内仍是 LIFO 栈：

- `Push(vm, cover)`：`cover` 默认 `Pause`，`Deactivate` 当前顶层或被盖住的屏；`Continue` 则下层继续跑。加入 `Layers` 并 `Activate` 新层。
- `Back()`：弹出顶层并 `Dispose`。仅当该层是 `Pause` 时，才 `Activate` 新顶或 covered。
- `Clear(resumeCovered)`：关掉全部。`resumeCovered: false` 给换屏用；若第一层是 `Continue`，也不会 Activate 旧屏。
- `HasOverlay`：该带是否非空。

单独使用 `Overlay` 时，Godot 槽必须是普通 `Control`（不要 `Container`），否则无法叠满。空槽 `MouseFilter = Ignore`，有层且挡点击时 `Stop`。

## OverlayHost

`OverlayHost` 实现 `IOverlay`：无参 `Push` / `Layers` / `Back` 仍指向 `OverlayLayer.Popup`，已有子页可以继续只拿 `IOverlay`。需要点名带时拿 `IOverlayHost`。

每个 `OverlayLayer` 是独立栈，不是同一个栈里的插入位置，也不是 Prism Region（单槽替换）。

内置带（构造时自动注册，`Order` 间隔 100）：

- `Popup`（0）：默认面板。`Pause`，参与 Back，槽非空时挡点击。
- `Modal`（100）：在 Popup 之上。`Pause`，参与 Back，挡点击。
- `Toast`（200）：在 Modal 之上。`Continue`，**不**参与 Back，槽始终 `Ignore`。

开发者在壳构造期 `Register`：

```csharp
host.Register(new OverlayLayer("Banner", order: 50, handlesBack: false,
    defaultCover: CoverMode.Continue, blocksPassThrough: false));
host.Push(new BannerViewModel(host), bannerLayer);
```

同名或同 `Order` 抛错。第一次 `Push` 之后再 `Register` 抛错。未知带 `Push` 抛错。

- `Push(vm, layer, cover?)`：`cover` 缺省用该带 `DefaultCover`。
- `Band(layer)`：该带的 `IOverlay`（绑定 `Layers` 用）。
- `Back()`：从 Order 高到低找第一个 `HandlesBack` 且非空的带，弹它。Toast 被跳过。
- `Close(vm)`：在所有带里查找。面板按钮应 `Close(this)`，不要假设自己是视觉顶层。
- `Clear`：清所有带（换屏仍清干净，Toast 一并关掉）。
- `HasOverlay`：任一带非空。`HasBackableOverlay`：是否有可 Back 的带（壳返回按钮用这个）。

带内 Pause/Continue 仍由该带的 `Overlay` 记录。带间激活由 Host `Reconcile`：被更高带且该层 `Pause` 盖住的项不 `Activate`；`Continue` 带（Toast）不暂停下面。向较低带 Push 时，若上方已有 Pause 的 Modal，新项不会进前台。

## Interaction

`IInteraction` 是请求/应答，不是 Messenger。页 VM 构造期注入，和 `INavigator` 一样；不要全局 DialogService，也不要按字符串名弹窗。

- `Run(dialog, layer?)`：应用提供 `DialogViewModel<T>`。默认推进 `OverlayLayer.Modal`。按钮只 `Complete(result)`，**不**拿 `IOverlay`。未回答就被关掉（壳 Back、换屏 `Clear`、取消 token）时用派生类的 `Dismissed`，不抛 `OperationCanceledException`。
- `Interaction` 包装 `IOverlayHost`：Push → await `Completion` → 对话框还活着则 `Close`。

是否确认、文件选择等都是应用的对话框 VM。Showcase 的 [`ConfirmViewModel`](../samples/Showcase/Navigation/Confirm/ConfirmViewModel.cs) 演示 Yes/No；场景由宿主 `ViewRegistry.Register<ConfirmViewModel>(...)`。Todo 删除走 `await Run`。

```csharp
if (!await interaction.Run(new ConfirmViewModel("Delete", $"Delete '{item.Title.Value}'?")))
{
    return;
}
```

## BindView

父场景里已经摆好的子 TelepathView（HUD、侧栏）是绑定目标，不是空槽。不要用 Overlay 带做持久 HUD。

- `BindContent`：按注册表 **实例化**，换页默认 `QueueFree`（`IViewTransition` 则先退场）。
- `BindView`：节点留在父场景里，只注入父 VM 上的子 VM。换 VM 只重绑。`null` 解绑并隐藏。
- **不** `Activate`，**不** `Dispose` 注入的 VM。
- 子先 `Ready`（可能先 `CreateViewModel()` 给 F6），父 `OnBind` 再注入：[`ViewLifecycle`](../src/Telepath.Godot/View/ViewLifecycle.cs) setter 丢掉 dummy 再接线。

场景：`kind: View, member: Status, path: %Status`。`[BindTo(..., Kind = LinkKind.View)]` 或 `bindings.BindView(vm.Status, _status.View())`。

Showcase 壳头的 [StatusView](../samples/Showcase/Shell/StatusView.tscn) 显示当前页名。

## Godot 宿主

`Telepath.Godot` **不**提供挂到场景上的 `ScreenHost : Control`。宿主是组合对象：

1. 显式 `ViewRegistry.Register<TViewModel>(scenePath)`。不扫 `[TelepathView]`。
2. `bindings.BindContent(vm.ActiveItem, slot.Content(registry, presented))`。屏幕要播遮盖时，和 Overlay 共用一份 [`PresentedViews`](../src/Telepath.Godot/Presentation/PresentedViews.cs)。
3. `bindings.BindOverlayHost(vm.Overlay, overlayRoot, registry, presented)`。根必须是普通 `Control`（不要 `Container`）。Presenter 按 `Bands` 建子槽，sibling 顺序 = `Order`。不要用 `CanvasLayer`。
4. 进树前写入 `ITelepathView.ViewModel`。Ready 时跳过 `CreateViewModel()`。
5. 单槽换页与 Overlay 关层默认立刻 `QueueFree`。实现了 [`IViewTransition`](#iviewtransition) 的视图：先解绑再播退场，结束才 `QueueFree`。**不** `Dispose` VM。
6. 不要用 `ChangeSceneToPacked` 当导航。

进树顺序与列表项相同：注入 → `AddChild` → Ready → `OnBind`。

被导航的页仍可保留 `CreateViewModel()`，以便 F6 单场景；壳里走注入。只作为子页出现的 View 可以像 `TodoItemView` 那样在未注入时抛错。

```csharp
private void OnBind(ShellViewModel vm, BindingSet bindings)
{
    var registry = new ViewRegistry()
        .Register<DirectoryViewModel>("res://Navigation/Directory/DirectoryView.tscn")
        .Register<AboutViewModel>("res://Navigation/About/AboutView.tscn")
        .Register<ToastViewModel>("res://Navigation/Toast/ToastView.tscn")
        .Register<ConfirmViewModel>("res://Navigation/Confirm/ConfirmView.tscn");
    var presented = new PresentedViews();
    bindings.BindContent(vm.ActiveItem, _content.Content(registry, presented));
    bindings.BindOverlayHost(vm.Overlay, _overlay, registry, presented);
    if (vm.ActiveItem.Value is null)
    {
        vm.Navigate(new DirectoryViewModel(vm, vm.Overlay, vm.Interaction));
    }
}
```

空槽 `MouseFilter = Ignore`。`BlocksPassThrough` 的带非空时该槽 `Stop`；Toast / Banner 槽始终 `Ignore`，由面板自己挡点击。

壳场景把 `BackCommand` 绑到返回按钮；内容槽和 Overlay 根只在 `OnBind` 里接线。Showcase 的 Overlay pause 页用节拍验证 Cover：`Pause` 后计数停，`Continue` 后计数不停；Modal 永远画在 Popup 之上；Toast 不暂停计数，壳 Back 也不关它。Pause 页、About、Confirm 都实现了 `IViewCoverTransition`：Toast / About 盖住时底下那页变暗；About 上再开 Modal / Toast 时，被盖住的 Overlay 面板同样变暗。

## IViewTransition

打开 / 关闭动画由 **View** 播（场景里 `AnimationPlayer` 或 C# Tween），不在 Core，也不按 `OverlayLayer` 配默认淡入淡出。`Navigate` / `Close` / `Push` / `Back` 仍是同步 `void`：栈先改完、VM 先 `Dispose`，Presenter 把已解绑的节点当幽灵播退场。

宿主 View 按需实现 [`IViewTransition`](../src/Telepath.Godot/Presentation/Transition/IViewTransition.cs)。未实现则进 / 退场立即完成，Showcase 仍是瞬切。生成器**不**给每个 View 生成空方法，也不按节点名自动找 `AnimationPlayer`。

```csharp
public interface IViewTransition
{
    Task PlayEnterAsync(CancellationToken cancellationToken);
    Task PlayExitAsync(CancellationToken cancellationToken);
}
```

| | `IActivatable` | `IViewTransition` | `IViewCoverTransition` |
|---|---|---|---|
| 谁实现 | ViewModel（可选） | View（可选） | View（可选） |
| 何时 | 进 / 离前台 | 节点进树并绑定之后 / 解绑之后 | 上方还有 Overlay / 上方清空 |
| 用途 | 停轮询、停请求 | 播进场 / 退场 | 被盖住页变暗、缩小 |
| 结束 | `Activate` ≠ 进场播完 | 退场结束才 `QueueFree` | 揭开结束页仍在树上 |

顺序：

- **进场**：`Inject` → `AddChild`（Ready / `OnBind`）→ `PlayEnterAsync`。`Activate` 与进场重叠。`PlayEnterAsync` **必须同步写下第一帧**（例如立刻 `Modulate.A = 0`），否则会闪一帧全不透明。
- **退场**（单层 `Close` / `Back` / 换页替换）：Presenter 立刻 `ViewModel = null` 并忽略点击，再 `PlayExitAsync`。退场**不得读 VM**。播完（或实现未提供）才 `QueueFree`。
- **跳过动画**：`Clear` / `Reset` / 槽 `Detach`（换屏时的 `Overlay.Clear`）取消进行中的过渡并立刻 `QueueFree`。

换页时旧页退场与新页进场**并行**，槽里可短暂两个 child。连续 `Navigate` A→B→C 会取消 A 的退场并强制释放，避免幽灵堆叠。Overlay 增层播进场，删层播该层退场；被盖住的层若实现了 [`IViewCoverTransition`](#iviewcovertransition) 则并行播遮盖，否则视觉不变。退场幽灵 `MouseFilter = Ignore`（含子节点），且**不计入** Overlay 槽是否挡点击。

`BindView` 不走这套：节点常驻，不实例化、不 `QueueFree`。

`IViewTransition` 不规定怎么播。场景里预做好的 clip 用 [`AnimationPlayerTransition`](../src/Telepath.Godot/Presentation/Transition/AnimationPlayerTransition.cs)；代码里拼的位移 / 淡入淡出用 [`TweenTransition`](../src/Telepath.Godot/Presentation/Transition/TweenTransition.cs)。两者都只负责「等到结束或取消」，不提供默认曲线。

```csharp
public Task PlayEnterAsync(CancellationToken cancellationToken)
    => AnimationPlayerTransition.PlayAsync(_player, "enter", cancellationToken);

public Task PlayExitAsync(CancellationToken cancellationToken)
{
    var tween = CreateTween();
    tween.TweenProperty(this, "modulate:a", 0.0f, 0.2);
    return TweenTransition.PlayAsync(tween, cancellationToken);
}
```

进场若用 Tween，仍要在 `CreateTween` **之前**同步写下第一帧（例如 `Modulate = new Color(Modulate, 0)`），再 `TweenProperty` 到目标值。

## IViewCoverTransition

被盖住的页要自己播动画时，实现 [`IViewCoverTransition`](../src/Telepath.Godot/Presentation/Transition/IViewCoverTransition.cs)。这不是 `IActivatable`：Toast 的 `Continue` 不停 VM，但 z 序上仍盖住屏幕，`Covered` 仍包含它。也不是 `IViewTransition`：视图仍绑定，**可以读 VM**。

`OverlayHost.Covered` 按带的 `Order` 和带内 `Layers` 从低到高扫，**上方还有至少一层 Overlay** 的 VM 进入集合，与 `CoverMode` 无关。屏幕和更低的 Overlay 层都会进；顶层自己不进。壳把同一份 `PresentedViews` 传给 `Content` 和 `BindOverlayHost`；HUD 的 `BindView` 不进这张表。Overlay 视图和屏幕一样，实现 `IViewCoverTransition` 就会在被更高带或同带上层盖住时播遮盖。

Presenter **不等** 遮盖结束再 `Push`：新层 `PlayEnterAsync` 与底下 `PlayCoverAsync` 同帧并行。`PlayCoverAsync` **必须同步写下第一帧**。未实现则视觉不变。生成器不生成空方法。

换屏 `Clear(resumeCovered: false)` 把当前屏幕暂时留在 `Covered` 里，直到 `CurrentScreen` 换成新页，避免先揭开再 `PlayExit`。退场 / `Abort` 会取消进行中的 cover。

```csharp
public Task PlayCoverAsync(CancellationToken cancellationToken)
    => FadeTransition.CoverAsync(this, cancellationToken);

public Task PlayUncoverAsync(CancellationToken cancellationToken)
    => FadeTransition.UncoverAsync(this, cancellationToken);
```

Showcase：[PauseDemoView](../samples/Showcase/Navigation/Pause/PauseDemoView.cs)（屏幕）、[AboutView](../samples/Showcase/Navigation/About/AboutView.cs) / [ConfirmView](../samples/Showcase/Navigation/Confirm/ConfirmView.cs)（Overlay）。

不做：Prism Region、字符串路由表、Messenger、全局 DialogService、按名 `ShowDialog("Confirm")`、自研 DI、Autoload 组合根 / 壳工厂（对象图由宿主 `CreateViewModel` 和构造注入拼）、离开守卫 / `IGuardClose`（何时允许离开是壳的策略，问一句走 `IInteraction`）、文件选择（原生 `FileDialog` 或自定义 `DialogViewModel<T>` 走 `Run`，不进 Core）、把 `IActivatable` 塞进每个 ViewModel、Core 等待动画或异步 `Close`、框架默认淡入淡出、把过渡塞进 Ready / ExitTree / Predelete、按 z 插入同一个 Overlay 栈、把 HUD 做成 Overlay 带（持久 HUD 走 `BindView`）。
