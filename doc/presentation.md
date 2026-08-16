# Presentation

单槽换屏：导体拥有当前页 ViewModel 与返回栈，Godot 宿主按注册表实例化场景并注入。
导航状态在 ViewModel 上，不是 Autoload 服务定位器。

```
src/Telepath.Core/Presentation/
  INavigator.cs       子页请求 Navigate / Back / CloseSelf
  IConductor.cs       当前页、Close、栈
  IActivatable.cs     可选进前台 / 离前台钩子
  Conductor.cs        默认实现：压栈、弹出、离栈即 Dispose
src/Telepath.Core/Binding/
  ContentTarget.cs              单项槽（对标 CollectionTarget）
  ContentBindingExtensions.cs   BindContent
src/Telepath.Godot/Presentation/
  ViewRegistry.cs         ViewModel 类型 → PackedScene
  ContentPresenter.cs     实例化、注入、换页时 QueueFree 视图
  GodotContentTargets.cs  Control.Content(registry)
```

示例：[Shell](../samples/Showcase/Shell/)（目录页跳进四个现有 App，壳上 Back 返回）。

## 两条寿命

资源寿命仍由 [`ViewLifecycle`](../src/Telepath.Godot/View/ViewLifecycle.cs) 驱动，**不要**把打开 / 关闭 / 暂停塞进 Ready / ExitTree / Predelete。

| | 资源寿命 | 呈现寿命 |
|---|---|---|
| 谁驱动 | Godot 节点通知 | 导体 |
| 进 | Ready：创建或拿到 VM，接绑定 | `IActivatable.Activate`：进前台 |
| 出树 / 被盖住 | ExitTree：**只断绑定** | `Deactivate`：失焦；默认**不断绑定、不 Dispose** |
| 结束 | Predelete：仅自有 VM `Dispose` | Close / Back：出栈；默认 Dispose 离栈页 |

打开 ≠ `new`，关闭 ≠ `Dispose`，暂停 ≠ `ExitTree`。第一期是单槽换屏，没有遮挡栈；`IActivatable` 先留接口，Overlay 以后复用同一套钩子和 `ContentPresenter`。

## 导体

`Conductor` 继承 `ViewModel`，实现 `IConductor` / `INavigator`。

- `Navigate(vm)`：若已有当前页则 `Deactivate` 并压栈，再把 `ActiveItem` 设为 `vm` 并 `Activate`。导体取得所有权。
- `Back()`：弹出；`Deactivate` 并 `Dispose` 当前页，恢复上一页并 `Activate`。栈空时返回 `false`。
- `Close(vm)`：当前页等价于 `Back`（栈空则清空槽）；栈中的页直接移出并 `Dispose`。
- `CloseSelf()`：关闭当前页；无当前页则忽略。
- `CanGoBack` / `BackCommand`：给壳上的返回按钮。
- 同一实例已是当前页：`Navigate` 忽略。已在栈上：抛错（第一期不做缓存 / bring-to-front）。
- 导体 `Dispose` 时释放当前页和栈上剩余页。

子页不要认识壳类型。需要跳转时，构造期注入 `INavigator`（与 Todo 项注入 `Action` 相同）。

```csharp
public sealed class ShellViewModel : Conductor;

public sealed partial class DirectoryViewModel : ViewModel
{
    private readonly INavigator _navigator;

    public DirectoryViewModel(INavigator navigator) => _navigator = navigator;

    [Command]
    private void OnOpenCounter() => _navigator.Navigate(new CounterViewModel());
}
```

壳 View 先 `BindContent` 再 `Navigate` 首页，这样第一页的 `Activate` 也发生在视图进树并接绑定之后。出树再进树时 `ActiveItem` 仍在，只重新 `Present`，不再 `Navigate`。

`IActivatable` 是可选的，**不**进 `IViewModel`。需要停轮询、停请求的页再实现。导体在切换 `ActiveItem` 时调用：先 `Deactivate` 旧页，改属性（宿主同步换视图），再 `Activate` 新页。此时新页的 View 若已进树，绑定已经接上。

## Godot 宿主

`Telepath.Godot` **不**提供挂到场景上的 `ScreenHost : Control`。宿主是组合对象，和列表项同一套路：

1. 显式 `ViewRegistry.Register<TViewModel>(scenePath)`。第一期不扫 `[TelepathView]`（场景路径生成器给不出）。
2. `bindings.BindContent(vm.ActiveItem, slot.Content(registry))`。
3. `ContentPresenter` 在 `AddChild` **之前**写入 `ITelepathView.ViewModel`，再进树。Ready 时跳过 `CreateViewModel()`。
4. 换页时 `RemoveChild` + `QueueFree` 旧视图。**不** `Dispose` VM（导体才拥有）。
5. 不要用 `ChangeSceneToPacked` 当导航：会拆掉壳和 Autoload 以外的整棵树。

进树顺序与列表项相同：注入 → `AddChild` → Ready → `OnBind`。

被导航的页仍可保留 `CreateViewModel()`，以便 F6 单场景；壳里走注入。只作为子页出现的 View（目录）可以像 `TodoItemView` 那样在未注入时抛错。

```csharp
[TelepathView<ShellViewModel>]
public partial class ShellView : Control
{
    [NodeInject("%Content")]
    private Control _content = null!;

    public override partial void _Notification(int what);

    private ShellViewModel CreateViewModel() => new();

    private void OnBind(ShellViewModel vm, BindingSet bindings)
    {
        var registry = new ViewRegistry()
            .Register<DirectoryViewModel>("res://Shell/DirectoryView.tscn")
            .Register<CounterViewModel>("res://CounterApp/CounterView.tscn");
        bindings.BindContent(vm.ActiveItem, _content.Content(registry));
        if (vm.ActiveItem.Value is null)
        {
            vm.Navigate(new DirectoryViewModel(vm));
        }
    }
}
```

壳场景把 `BackCommand` 绑到返回按钮；内容槽只在 `OnBind` 里 `BindContent`，没有场景 `Kind`。

## 以后

- Overlay：同一注册表，多层 Control；被盖住的页 `Deactivate`，默认不断绑定
- Interaction：确认框 / 文件选择，对话框场景同样注入
- `BindView`：父场景里已有子 TelepathView 节点，注入父 VM 上的某个子 VM 属性
- 装配：Autoload 组合根给壳工厂；`CreateViewModel()` 留作逃逸口

不做：Prism Region、字符串路由表、Messenger、自研 DI、把 `IActivatable` 塞进每个 ViewModel。
