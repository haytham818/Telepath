# Presentation

导航状态在 ViewModel 上，不是 Autoload 服务定位器。两种宿主策略：

- **单槽换屏**（`Conductor`）：当前页独占槽；换页 `QueueFree` 旧视图，VM 可留在返回栈。
- **Overlay 栈**（`Overlay`）：多层视图同时留在树上并保持绑定；只有顶层 `Activate`，被盖住的页 `Deactivate` 但不销毁。

```
src/Telepath.Core/Presentation/
  INavigator.cs       子页请求 Navigate / Back / CloseSelf
  IConductor.cs       当前页、Close、栈
  IOverlay.cs         叠层 Push / Back / CloseSelf / Clear
  IActivatable.cs     可选进前台 / 离前台钩子
  Conductor.cs        单槽：压栈、弹出、离栈即 Dispose
  Overlay.cs          多层：留视图、遮挡暂停
src/Telepath.Core/Binding/
  ContentTarget.cs / ContentBindingExtensions.cs   BindContent
  OverlayTarget.cs / OverlayBindingExtensions.cs   BindOverlay
src/Telepath.Godot/Presentation/
  ViewRegistry.cs         ViewModel 类型 → PackedScene
  ContentPresenter.cs     换页时 QueueFree 旧视图
  OverlayPresenter.cs     增层进树、删层只 QueueFree 该视图
  GodotContentTargets.cs  Control.Content(registry)
  GodotOverlayTargets.cs  Control.Overlays(registry)
```

示例：[Shell](../samples/Showcase/Shell/)（目录进四个 App + Overlay pause 演示；壳上 Back 先关 Overlay）。

## 两条寿命

资源寿命仍由 [`ViewLifecycle`](../src/Telepath.Godot/View/ViewLifecycle.cs) 驱动，**不要**把打开 / 关闭 / 暂停塞进 Ready / ExitTree / Predelete。

| | 资源寿命 | 呈现寿命 |
|---|---|---|
| 谁驱动 | Godot 节点通知 | 导体 / Overlay |
| 进 | Ready：创建或拿到 VM，接绑定 | `IActivatable.Activate`：进前台 |
| 出树 / 被盖住 | ExitTree：**只断绑定** | `Deactivate`：失焦或被遮挡；默认**不断绑定、不 Dispose** |
| 结束 | Predelete：仅自有 VM `Dispose` | Close / Back：出栈；默认 Dispose 离栈页 |

打开 ≠ `new`，关闭 ≠ `Dispose`，暂停 ≠ `ExitTree`。

单槽换页会拆掉旧视图（出树断绑定），VM 仍可在返回栈上；再 `Back` 时重新实例化视图并注入同一 VM。Overlay 相反：被盖住的视图留在树上，绑定不断，只 `Deactivate`。

## 导体

`Conductor` 继承 `ViewModel`，实现 `IConductor` / `INavigator`。

- `Navigate(vm)`：若已有当前页则 `Deactivate` 并压栈，再把 `ActiveItem` 设为 `vm` 并 `Activate`。导体取得所有权。
- `Back()`：弹出；`Deactivate` 并 `Dispose` 当前页，恢复上一页并 `Activate`。栈空时返回 `false`。
- `Close(vm)`：当前页等价于 `Back`（栈空则清空槽）；栈中的页直接移出并 `Dispose`。
- `CloseSelf()`：关闭当前页；无当前页则忽略。
- `CanGoBack` / `BackCommand`：给壳上的返回按钮。`ComputeCanGoBack` 可覆盖（壳把 Overlay 算进去）。
- 同一实例已是当前页：`Navigate` 忽略。已在栈上：抛错（不做缓存 / bring-to-front）。
- 导体 `Dispose` 时释放当前页和栈上剩余页。

子页不要认识壳类型。需要跳转时，构造期注入 `INavigator`（与 Todo 项注入 `Action` 相同）。

```csharp
public sealed class ShellViewModel : Conductor
{
    public Overlay Overlay { get; }

    public ShellViewModel()
    {
        Overlay = Track(new Overlay(() => ActiveItem.Value));
        Track(Overlay.HasOverlay.Subscribe(_ => UpdateCanGoBack()));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack()
        => Overlay.HasOverlay.Value || base.ComputeCanGoBack();
}
```

壳 View 先 `BindContent` / `BindOverlay` 再 `Navigate` 首页，这样第一页的 `Activate` 也发生在视图进树并接绑定之后。出树再进树时 `ActiveItem` 仍在，只重新 `Present`，不再 `Navigate`。

`IActivatable` 是可选的，**不**进 `IViewModel`。需要停轮询、停请求的页再实现。切换时：先 `Deactivate` 旧前台，改栈（宿主同步改视图），再 `Activate` 新前台。

## Overlay

`Overlay` 不实现 `INavigator`，避免面板 `Back` 误伤屏幕栈。面板拿 `IOverlay`。

- `Push(vm)`：`Deactivate` 当前顶层；若这是第一层，再 `Deactivate` 被盖住的屏（构造期 `Func<IViewModel?>? covered`）。加入 `Layers` 并 `Activate`。
- `Back()`：弹出顶层并 `Dispose`；若还有层则 `Activate` 新顶，否则 `Activate` covered。
- `Clear(resumeCovered)`：关掉全部。`resumeCovered: false` 给换屏用，避免多一次 Activate 旧屏。
- `HasOverlay`：给壳 Back 和空槽鼠标穿透。

Godot 槽必须是普通 `Control`（不要 `Container`），否则无法叠满。空槽 `MouseFilter = Ignore`，有层时 `Stop`。

## Godot 宿主

`Telepath.Godot` **不**提供挂到场景上的 `ScreenHost : Control`。宿主是组合对象：

1. 显式 `ViewRegistry.Register<TViewModel>(scenePath)`。不扫 `[TelepathView]`。
2. `bindings.BindContent(vm.ActiveItem, slot.Content(registry))`。
3. `bindings.BindOverlay(vm.Overlay.Layers, overlaySlot.Overlays(registry))`。
4. 进树前写入 `ITelepathView.ViewModel`。Ready 时跳过 `CreateViewModel()`。
5. 单槽换页 `QueueFree` 旧视图；Overlay 只 `QueueFree` 被关掉的那一层。**不** `Dispose` VM。
6. 不要用 `ChangeSceneToPacked` 当导航。

进树顺序与列表项相同：注入 → `AddChild` → Ready → `OnBind`。

被导航的页仍可保留 `CreateViewModel()`，以便 F6 单场景；壳里走注入。只作为子页出现的 View 可以像 `TodoItemView` 那样在未注入时抛错。

```csharp
private void OnBind(ShellViewModel vm, BindingSet bindings)
{
    var registry = new ViewRegistry()
        .Register<DirectoryViewModel>("res://Shell/DirectoryView.tscn")
        .Register<AboutViewModel>("res://Shell/AboutView.tscn");
    bindings.BindContent(vm.ActiveItem, _content.Content(registry));
    bindings.BindOverlay(vm.Overlay.Layers, _overlay.Overlays(registry));
    if (vm.ActiveItem.Value is null)
    {
        vm.Navigate(new DirectoryViewModel(vm, vm.Overlay));
    }
}
```

壳场景把 `BackCommand` 绑到返回按钮；内容槽和 Overlay 槽只在 `OnBind` 里接线。Showcase 的 Overlay pause 页用节拍验证：盖上 About 后计数停、视图仍在；关掉后继续。

## 以后

- Interaction：确认框 / 文件选择，对话框场景同样注入
- `BindView`：父场景里已有子 TelepathView 节点，注入父 VM 上的某个子 VM 属性
- 装配：Autoload 组合根给壳工厂；`CreateViewModel()` 留作逃逸口

不做：Prism Region、字符串路由表、Messenger、自研 DI、把 `IActivatable` 塞进每个 ViewModel。
