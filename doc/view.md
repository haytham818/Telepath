# View

View 是宿主程序集中的具体 Godot `Control`。Telepath 用组合对象管理绑定和
ViewModel 寿命，不在库程序集中声明 Godot View 基类。

```
src/Telepath.Core/Binding/                       BindingSet 袋 + OneWay / TwoWay / BindCommand
src/Telepath.Godot/Binding/GodotBindings.cs      控件适配
src/Telepath.Godot/View/ViewLifecycle.cs         ViewModel / 绑定寿命
src/Telepath.Godot/View/TelepathViewAttribute.cs View 标记
src/Telepath.Godot/View/LinkToAttribute.cs       声明式绑定
src/Telepath.Godot/View/LinkKind.cs              覆盖推断
src/Telepath.SourceGenerator/View/               诊断、校验、源码渲染
```

示例：[CounterApp](../samples/Showcase/CounterApp/CounterView.cs)（无参命令），[SearchApp](../samples/Showcase/SearchApp/SearchView.cs)（`ReactiveCommand<T>` + `Parameter`）。

具体 View 必须：

- 在 Godot 宿主程序集中直接继承非泛型 `Control`
- 标记 `[TelepathView<TViewModel>]`
- 声明 `public override partial void _Notification(int what);`
- 提供 `CreateViewModel()`
- 用 `[LinkTo]` 声明绑定，和/或手写 `OnBind(...)`；可选提供 `OnReady()`

`_Notification` 的声明必须写在用户源码中，Godot 的源生成器才能登记该回调。
Telepath 生成器实现它并转发给 `ViewLifecycle<TViewModel>`；Godot 的
`MethodName`、方法表和调用桥仍全部由当前 Godot SDK 生成。

库中没有继承 `GodotObject` 的 View 类型，也没有构造泛型 Godot 脚本，因此不会
把 `View` / `View<TViewModel>` 注册进 `ScriptTypeBiMap`。

## 寿命

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` | 若未注入则 `CreateViewModel()` 一次 | 解析 `[LinkTo]` 节点后 `OnBind` |
| `_EnterTree`（已 Ready、无绑定） | 不 new | 再次接线 |
| `_ExitTree` | **不** `Dispose` | `BindingSet.Dispose` |
| `NotificationPredelete` | `ViewModel.Dispose()` | 已在出树时断开 |

不要 `viewModel.AddTo(node)`。订阅进 `BindingSet`，或 `bindings.Add(...)`。

## API

- `ViewModel`：可注入；`_Ready` 时仍为空才 `CreateViewModel()`
- `[LinkTo(nodePath, member)]`：生成 `GetNode` 与对应 `BindXxx`；可用 `Kind` 覆盖推断，`Parameter` 给带参命令取值
- `OnReady()`：可选；在生成的节点解析之后调用
- `OnBind(vm, bindings)`：可选；在声明式绑定之后调用，用于额外接线。`bindings` 是 `Telepath.Core.BindingSet`
- `_Notification`：只声明 partial 方法，不要自行实现或覆盖其他生命周期方法

`BindingSet` 只收集一次进树周期的订阅。接线走三条原语：`OneWay`、`TwoWay`、`BindCommand`。Godot 控件方法是薄适配；`Observable<T>` 一向，`BindableReactiveProperty<T>` 双向。

## `[LinkTo]` 推断

按最具体控件类型选择方法。`CheckBox` / `OptionButton` 优先于 `BaseButton`。

| 控件 | 生成 | 默认方向 |
|------|------|----------|
| `Label` / `RichTextLabel` | `BindText` | 一向 |
| `LineEdit` / `TextEdit` | `BindText` | `BindableReactiveProperty<string>` 双向，否则一向 |
| `CheckBox` / `CheckButton` | `BindToggle` | 双向 `bool` |
| `OptionButton` | `BindSelected` | 双向 `long` |
| 其他 `BaseButton` | `BindCommand` | 按下 `Execute`，`CanExecute` → `Disabled` |
| `Range`（Slider / SpinBox / ProgressBar / ScrollBar） | `BindValue` | `BindableReactiveProperty<double>` 双向，否则一向 |
| 普通 `Control` | 报 TPV008 | 必须设 `Kind` |

`Visible` / `Disabled` 不靠类型猜：

```csharp
[LinkTo("%Panel", nameof(Vm.ShowAdvanced), Kind = LinkKind.Visible)]
private Control _panel = null!;
```

- `BindVisible` → `CanvasItem.Visible`
- `BindDisabled` → `BaseButton.Disabled`（Godot 的 `Control` 没有 Disabled）
- `Kind` 也可覆盖推断，例如 CheckBox 当命令：`Kind = LinkKind.Command`
- 带参命令：按钮按下时从另一个控件取值。`LineEdit` + `Kind = Command` 则走 `TextSubmitted`，不必写 `Parameter`

```csharp
[LinkTo("%Query", nameof(SearchViewModel.Query))]
private LineEdit _query = null!;

[LinkTo("%Search", nameof(SearchViewModel.SearchCommand), Parameter = nameof(_query))]
private Button _search = null!;
```

生成 `bindings.BindCommand(vm.SearchCommand, _search, () => _query.Text)`。`Parameter` 取值：文本控件 `.Text`，开关 `.ButtonPressed`，`Range` `.Value`，`OptionButton` `.Selected`。

```csharp
[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    [LinkTo("%CountLabel", nameof(CounterViewModel.CountText))]
    private Label _countLabel = null!;

    [LinkTo("%IncrementButton", nameof(CounterViewModel.IncrementCommand))]
    private Button _incrementButton = null!;

    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
```

尚未纳入：转换器、列表。ViewModel 契约见 [viewmodel.md](viewmodel.md)，R3 胶水见 [r3-godot.md](r3-godot.md)。
