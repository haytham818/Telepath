# View

View 是宿主程序集中的具体 Godot `Control`。Telepath 用组合对象管理绑定和
ViewModel 寿命，不在库程序集中声明 Godot View 基类。

```
src/Telepath.Core/Binding/                       BindingSet、BindingTarget、CollectionTarget、Bind / BindCommand / BindItems
src/Telepath.Godot/Binding/GodotTargets.cs       控件 → BindingTarget（.Text() / .Value() / .Selected() 等）
src/Telepath.Godot/Binding/GodotCollectionTargets.cs  ItemList / OptionButton / Container → CollectionTarget
src/Telepath.Godot/Binding/GodotCommands.cs      按钮 / LineEdit 命令
src/Telepath.Godot/View/ViewLifecycle.cs         ViewModel / 绑定寿命
src/Telepath.Godot/View/ITelepathView.cs         可注入 ViewModel 的 View 契约
src/Telepath.Godot/View/TelepathViewAttribute.cs View 标记
src/Telepath.Godot/Binding/Attrtbutes/NodeInjectAttribute.cs  节点注入
src/Telepath.Godot/Binding/Attrtbutes/BindToAttribute.cs      声明式绑定
src/Telepath.Godot/Binding/Attrtbutes/LinkKind.cs             覆盖推断
src/Telepath.SourceGenerator/View/               诊断、校验、源码渲染
```

示例：[CounterApp](../samples/Showcase/CounterApp/CounterView.cs)（无参命令），[SearchApp](../samples/Showcase/SearchApp/SearchView.cs)（`ReactiveCommand<T>` + `Parameter`），[ListApp](../samples/Showcase/ListApp/ListView.cs)（`ItemList`），[TodoApp](../samples/Showcase/TodoApp/TodoListView.cs)（容器 + 子 View）。

具体 View 必须：

- 在 Godot 宿主程序集中直接继承非泛型 `Control`
- 标记 `[TelepathView<TViewModel>]`
- 声明 `public override partial void _Notification(int what);`
- 提供 `CreateViewModel()`
- 用 `[NodeInject]` / `[BindTo]` 声明注入与绑定，和/或手写 `OnBind(...)`；可选提供 `OnReady()`

`_Notification` 的声明必须写在用户源码中，Godot 的源生成器才能登记该回调。
Telepath 生成器实现它并转发给 `ViewLifecycle<TViewModel>`；Godot 的
`MethodName`、方法表和调用桥仍全部由当前 Godot SDK 生成。

库中没有继承 `GodotObject` 的 View 类型，也没有构造泛型 Godot 脚本，因此不会
把 `View` / `View<TViewModel>` 注册进 `ScriptTypeBiMap`。

## 寿命

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` | 若未注入则 `CreateViewModel()` 一次 | 解析 `[NodeInject]` 节点后 `OnBind` |
| `_EnterTree`（已 Ready、无绑定） | 不 new | 再次接线 |
| `_ExitTree` | **不** `Dispose` | `BindingSet.Dispose` |
| `NotificationPredelete` | 自己 `CreateViewModel()` 的才 `ViewModel.Dispose()`；注入的不 Dispose | 已在出树时断开 |

不要 `viewModel.AddTo(node)`。订阅进 `BindingSet`，或 `bindings.Add(...)`。

## API

- `ViewModel`：可注入；`_Ready` 时仍为空才 `CreateViewModel()`。注入的 VM 在节点释放时不 `Dispose`
- `[NodeInject(nodePath)]`：生成 `GetNode`；同一字段只能一条
- `[BindTo(member)]`：生成 `Bind(source, target)`；必须搭配同字段的 `[NodeInject]`。可用 `Kind` 覆盖推断，`Parameter` 给带参命令取值，`Converter` 做类型转换。同一字段可叠多条
- 允许只有 `[NodeInject]`、没有 `[BindTo]`（仅注入，在 `OnBind` 手写接线）
- `OnReady()`：可选；在生成的节点解析之后调用
- `OnBind(vm, bindings)`：可选；在声明式绑定之后调用，用于额外接线。`bindings` 是 `Telepath.Core.BindingSet`
- `_Notification`：只声明 partial 方法，不要自行实现或覆盖其他生命周期方法

View 仍须提供 `OnBind` 和/或至少一条 `[BindTo]`（仅有注入、既无 BindTo 也无 OnBind 会报错）。

`BindingSet` 只收集一次进树周期的订阅。接线走 `Bind(source, BindingTarget)`（内部是 `OneWay` / `TwoWay`）、`BindCommand` 和 `BindItems`。Godot 只提供 Target 与命令适配；`Observable<T>` 一向，`BindableReactiveProperty<T>` 在目标支持 get/changed 时双向。

## `[BindTo]` 推断

按最具体控件类型选择方法。`CheckBox` / `OptionButton` 优先于 `BaseButton`。

| 控件 | 生成 | 默认方向 |
|------|------|----------|
| `Label` / `RichTextLabel` | `Bind(..., .Text())` | 一向；无 Converter 时 `ToStringConverter.Convert` |
| `LineEdit` / `TextEdit` | `Bind(..., .Text())` | `BindableReactiveProperty<string>` 双向，否则一向 |
| `CheckBox` / `CheckButton` | `Bind(..., .Toggle())` | 双向 `bool` |
| `OptionButton` | `Bind(..., .Selected())` | 双向 `long` |
| `ItemList` | 必须设 `Kind = Selected` | 双向 `long`（无选中为 `-1`）；项集合用手写 `BindItems` |
| 其他 `BaseButton` | `BindCommand` | 按下 `Execute`，`CanExecute` → `Disabled` |
| `Range`（Slider / SpinBox / ProgressBar / ScrollBar） | `Bind(..., .Value())` | `BindableReactiveProperty<double>` 双向，否则一向；`int` / `float` 隐式转 `double` |
| 普通 `Control` | 报 TPV008 | 必须设 `Kind` |

`Visible` / `Disabled` 不靠类型猜：

```csharp
[NodeInject("%Panel")]
[BindTo(nameof(Vm.ShowAdvanced), Kind = LinkKind.Visible)]
private Control _panel = null!;
```

- `.Visible()` → `CanvasItem.Visible`
- `.Disabled()` → `BaseButton.Disabled`（Godot 的 `Control` 没有 Disabled）
- `Kind` 也可覆盖推断，例如 CheckBox 当命令：`Kind = LinkKind.Command`
- 带参命令：按钮按下时从另一个控件取值。`LineEdit` + `Kind = Command` 则走 `TextSubmitted`，不必写 `Parameter`
- 同一控件要绑多条（例如 `LineEdit` 既双向文本又回车提交）时，在同一字段上叠 `[BindTo]`，不必再写 `OnBind`

```csharp
[NodeInject("%Query")]
[BindTo(nameof(SearchViewModel.Query))]
[BindTo(nameof(SearchViewModel.SearchCommand), Kind = LinkKind.Command)]
private LineEdit _query = null!;

[NodeInject("%Search")]
[BindTo(nameof(SearchViewModel.SearchCommand), Parameter = nameof(_query))]
private Button _search = null!;
```

生成 `bindings.Bind(vm.Query, _query.Text())`、`bindings.BindCommand(vm.SearchCommand, _query)`，以及 `bindings.BindCommand(vm.SearchCommand, _search, () => _query.Text)`。`Parameter` 取值：文本控件 `.Text`，开关 `.ButtonPressed`，`Range` `.Value`，`OptionButton` `.Selected`。

## 转换

Label / RichTextLabel 无 `Converter` 时生成 `ToStringConverter.Convert`（与显式 `ToStringConverter<T>` 同一实现）。Range 的 `int` / `float` 走 `IntToDoubleConverter` / `FloatToDoubleConverter`。LineEdit / TextEdit 不隐式 `ToString()`，非 `string` 必须给转换器。

自定义转换实现 `Telepath.Core.IValueConverter<TSource, TTarget>`（一向）或 `ITwoWayValueConverter<TSource, TTarget>`（双向，再实现 `ConvertBack`）。类型必须具体、非开放泛型、有公共无参构造。命令绑定不能带 `Converter`。内置：`ToStringConverter<T>`、`IntToDoubleConverter`、`FloatToDoubleConverter`；隐式绑定走同一套实现。

```csharp
public sealed class CountTextConverter : IValueConverter<int, string>
{
    public string Convert(int value) => $"Count: {value}";
}

[NodeInject("%CountLabel")]
[BindTo(nameof(CounterViewModel.Count), Converter = typeof(CountTextConverter))]
private Label _countLabel = null!;
```

生成 `bindings.Bind(vm.Count, _countLabel.Text(), new CountTextConverter())`。手写 `OnBind`：`bindings.Bind(vm.Count, _countLabel.Text(), c => $"Count: {c}")`。转换器与 `BindingTarget<T>` 是否匹配由 C# 重载决议检查；双向目标只实现一向转换器会走一向 `Bind`。

```csharp
[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    [NodeInject("%CountLabel")]
    [BindTo(nameof(CounterViewModel.Count), Converter = typeof(CountTextConverter))]
    private Label _countLabel = null!;

    [NodeInject("%IncrementButton")]
    [BindTo(nameof(CounterViewModel.IncrementCommand))]
    private Button _incrementButton = null!;

    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
```

## 列表

集合不走 `BindingTarget<T>`。`CollectionTarget<T>` 提供 `Reset` / `Insert` / `RemoveAt` / `Replace` / `Move`。`ObservableList<T>` 增量更新，`Observable<IReadOnlyList<T>>`（含 `BindableReactiveProperty`）每次整表 `Reset`。可选 `Func` / `IValueConverter` 把项转成目标类型。

Godot：`ItemList.Items()`、`OptionButton.Items()` 都是 `CollectionTarget<string>`。选中仍是标量：`ItemList` 要写 `Kind = LinkKind.Selected`，`OptionButton` 继续默认 `Selected`。容器模板用 `Container.Items(create)` 或 `Items<TView, TViewModel>(PackedScene)`，在 `AddChild` 之前注入项 ViewModel；出树时 `Detach` 会拆掉生成的子节点，不释放项 VM。`[BindTo]` 不生成 `BindItems`，在 `OnBind` 手写；可只用 `[NodeInject]` 注入容器。

```csharp
[NodeInject("%Items")]
[BindTo(nameof(ListViewModel.Selected), Kind = LinkKind.Selected)]
private ItemList _items = null!;

[NodeInject("%Choices")]
private OptionButton _choices = null!;

private void OnBind(ListViewModel vm, BindingSet bindings)
{
    bindings.BindItems(vm.Items, _items.Items());
    bindings.BindItems(vm.Items, _choices.Items());
}
```

```csharp
bindings.BindItems(vm.Items, _list.Items<TodoItemView, TodoItemViewModel>(ItemScene));
```

ViewModel 里 `ObservableList<T>` 手写，不要 `[Bindable]` 包成 `BindableReactiveProperty`。项若是 ViewModel，从集合移除和父 `OnDispose` 时由拥有者 `Dispose`。示例：[ListApp](../samples/Showcase/ListApp/ListView.cs)（`ItemList`），[TodoApp](../samples/Showcase/TodoApp/TodoListView.cs)（容器 + 子 View）。ViewModel 契约见 [viewmodel.md](viewmodel.md)，R3 胶水见 [r3-godot.md](r3-godot.md)。
