# View

View 是宿主程序集中的具体 Godot `Control`。Telepath 用组合对象管理绑定和
ViewModel 寿命，不在库程序集中声明 Godot View 基类。

推荐工作流：在编辑器里搭 UI、在 ViewModel 里写逻辑，再用 **Telepath Binding
Dock** 把控件连到属性 / 命令。绑定存在场景节点的 `metadata/telepath_bindings`
上；View.cs 只做类型胶水。`[NodeInject]` / `[BindTo]` / `OnBind` 仍可用，作为
转换器手写或特殊接线的逃逸口。

```
src/Telepath.Core/Binding/                       BindingSet、BindingTarget、CollectionTarget、Bind / BindCommand / BindItems
src/Telepath.Godot/Binding/GodotTargets.cs       控件 → BindingTarget（.Text() / .Value() / .Selected() 等）
src/Telepath.Godot/Binding/GodotCollectionTargets.cs  ItemList / OptionButton / Container → CollectionTarget
src/Telepath.Godot/Binding/GodotCommands.cs      按钮 / LineEdit 命令
src/Telepath.Godot/Binding/SceneBindingSchema.cs 场景 metadata 读写
src/Telepath.Godot/Binding/SceneBindingApplier.cs 运行时按场景条目接线
src/Telepath.Godot/View/ViewLifecycle.cs         ViewModel / 绑定寿命
src/Telepath.Godot/View/ITelepathView.cs         可注入 ViewModel 的 View 契约
src/Telepath.Godot/View/TelepathViewAttribute.cs View 标记
src/Telepath.Core/Presentation/                  导体、INavigator、可选 IActivatable（见 presentation.md）
src/Telepath.Godot/Presentation/                 ViewRegistry、ContentPresenter、BindContent 适配
src/Telepath.Godot/Addon/                        plugin.cfg、FrameProviderDispatcher.gd / .cs（不编进库）
src/Telepath.Godot/Editor/                       GDScript 插件外壳、Binding Dock 场景与 ViewModel（不编进 Telepath.Godot.dll）
src/Telepath.Godot/Binding/Attrtbutes/NodeInjectAttribute.cs  节点注入
src/Telepath.Godot/Binding/Attrtbutes/BindToAttribute.cs      声明式绑定
src/Telepath.Godot/Binding/Attrtbutes/LinkKind.cs             覆盖推断
src/Telepath.SourceGenerator/View/               诊断、校验、源码渲染
```

示例：[CounterApp](../samples/Showcase/CounterApp/)（场景绑定 + Converter，与目录换页淡入淡出），[SearchApp](../samples/Showcase/SearchApp/)（异步命令 + `ProgressBar` 的 `Value` / `Visible`），[ListApp](../samples/Showcase/ListApp/)（`ItemList`），[TodoApp](../samples/Showcase/TodoApp/TodoListView.tscn)（容器 + 子 View）。项模板 [TodoItemView](../samples/Showcase/TodoApp/TodoItemView.cs) 仍用 `[BindTo]` 作为逃逸口。换屏壳：[Shell](../samples/Showcase/Shell/)（Confirm / Toast / Banner / About 有打开关闭过渡）。

具体 View 必须：

- 在 Godot 宿主程序集中直接继承非泛型 `Control`
- 标记 `[TelepathView<TViewModel>]`
- 声明 `public override partial void _Notification(int what);`
- 提供 `CreateViewModel()`
- 用 Binding Dock 写场景绑定，和/或 `[NodeInject]` / `[BindTo]`，和/或手写 `OnBind(...)`；可选提供 `OnReady()`

`_Notification` 的声明必须写在用户源码中，Godot 的源生成器才能登记该回调。
Telepath 生成器实现它并转发给 `ViewLifecycle<TViewModel>`；Godot 的
`MethodName`、方法表和调用桥仍全部由当前 Godot SDK 生成。生成的
`__TelepathOnBind` 先 `SceneBindingApplier.Apply`，再跑 `[BindTo]`，再调用
用户 `OnBind`。

库中没有继承 `GodotObject` 的 View 类型，也没有构造泛型 Godot 脚本，因此不会
把 `View` / `View<TViewModel>` 注册进 `ScriptTypeBiMap`。

薄 View：

```csharp
[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
```

## 编辑器绑定器

宿主 addon 源码在 `src/Telepath.Godot/Addon/`（`plugin.cfg`、
`FrameProviderDispatcher`）和 `src/Telepath.Godot/Editor/`，**不**编进
`Telepath.Godot`。Showcase 把它们符号链接到 `samples/Showcase/addons/Telepath/`，
由宿主主程序集编译。分发时请同样链接或复制到自己工程的 `addons/Telepath/`。
Windows 上 git symlink 需要 `core.symlinks`，否则改为复制。

`FrameProviderDispatcher.gd` 是 Autoload 外壳，`FrameProviderDispatcher.cs`
是运行时子节点，都在 addon 根目录，不是 Editor 代码。编辑器插件入口是
GDScript；`selection_changed` 和 Dock 上的按钮 / 列表信号也在 GDScript 里转发。
C# 不要 `+=` Godot 信号，也不要用 `Callable.From` / R3 `FromEvent` 接编辑器控件
（godotengine/godot#81903）：引擎在 `ISerializationListener` **之前**快照
`ManagedCallable`，lambda 无法跨 ALC 恢复，表现为
`delegate_handle.value == nullptr` 并钉住程序集卸载。

选中带 `[TelepathView<T>]` 的节点（或其子节点）时，右侧 **UI绑定** dock
以绑定列表为主：点 `[+]` 添加，点一行展开编辑卡后「应用」或「删除」。
`Kind = Auto` 会显示推断结果（如 `Auto → Text`）；转换器 / 命令参数 / 项模板
只在当前 Kind 需要时出现。没有 `[BindTo]` 时不占只读区。连线写入根节点
metadata，改绑定不必等 C# 重建。改 `[Bindable]` / `[Command]` 后仍需重建，
Dock 才能看到新成员。

游戏 View 不要加 `[Tool]`：编辑器里它们是占位节点，Dock 靠脚本路径解析类型。
Dock 场景脚本是 GDScript（[`TelepathBindingDock.gd`](../src/Telepath.Godot/Editor/TelepathBindingDock.gd)）；
布局在 [`TelepathBindingDock.tscn`](../src/Telepath.Godot/Editor/TelepathBindingDock.tscn)。
唯一的 C# `[Tool]` 是 `BindingDockBridge`：只做反射、Undo 和一向属性同步，
**不**接 Godot 信号。逻辑在 `BindingDockViewModel`。不要在 C# 里 `new` 控件，
也不要 F6 该场景（依赖编辑器 API）。打开 tscn 调布局时不会创建 C# bridge；
只有插件 `Attach` 过的活 Dock 才会接线。

控件请设 **唯一名称**（`%CountLabel`）。路径一改，无唯一名的绑定会断。

同一控件不要既写场景绑定又写 `[BindTo]`。Dock 里 `[BindTo]` 条目只读。

## 场景绑定格式

键：`telepath_bindings`。值：字典数组。

| 字段 | 含义 |
|------|------|
| `path` | 节点路径，优先 `%UniqueName` |
| `member` | ViewModel 公开属性名（`Count`、`IncrementCommand`、`Items`） |
| `kind` | `Auto` / `Text` / `Command` / `Toggle` / `Value` / `Selected` / `Visible` / `Disabled` / `Items` / `View` |
| `converter` | 可选，`IValueConverter<,>` 的完整类型名 |
| `parameter` | 可选，带参命令取值的控件路径 |
| `item_view` | 可选，容器项 View 完整类型名 |
| `item_scene` | 可选，项模板 `res://...tscn` |

`Kind = Auto` 时按控件类型推断，规则与 `[BindTo]` 相同。Label 无 converter 时
`ToStringConverter`；Range 的 `int` / `float` 隐式转 `double`。容器 `Items` 用
`item_scene` 实例化子 View 并注入项 VM，不必在父 View 上 `[Export] PackedScene`。

## 寿命

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` | 若未注入则 `CreateViewModel()` 一次。之后再写入 `ViewModel` 会断旧绑、Dispose 自建 dummy、接新 VM | 场景绑定 + `[NodeInject]` 之后 `OnBind` |
| `_EnterTree`（已 Ready、无绑定） | 不 new | 再次接线 |
| `_ExitTree` | **不** `Dispose` | `BindingSet.Dispose` |
| `NotificationPredelete` | 自己 `CreateViewModel()` 的才 `ViewModel.Dispose()`；注入的不 Dispose | 已在出树时断开 |

不要 `viewModel.AddTo(node)`。订阅进 `BindingSet`，或 `bindings.Add(...)`。

这是**资源寿命**，不等于 UI 打开 / 关闭 / 被遮挡。导航由导体驱动，见 [presentation.md](presentation.md)。

## API

- `ViewModel`：可注入；`_Ready` 时仍为空才 `CreateViewModel()`。Ready 之后再赋值会重新接线。注入的 VM 在节点释放时不 `Dispose`；自建实例被注入替换时会 `Dispose`
- 场景 `telepath_bindings`：运行时 `GetNode` 并 `Bind` / `BindCommand` / `BindItems` / `BindView`
- `[NodeInject(nodePath)]`：生成 `GetNode`；同一字段只能一条
- `[BindTo(member)]`：生成 `Bind(source, target)` 或 `BindItems` / `BindView`；必须搭配同字段的 `[NodeInject]`。可用 `Kind` 覆盖推断，`Parameter` 给带参命令取值，`Converter` 做类型转换，`ItemView` / `ItemScene` 给容器模板。同一字段可叠多条
- `BindContent` / `BindOverlay` / `BindOverlayHost`：导体内容槽与 Overlay 带，见 [presentation.md](presentation.md)
- `BindView`：父场景里已有的子 TelepathView，见 [presentation.md](presentation.md)
- 允许只有 `[NodeInject]`、没有 `[BindTo]`（仅注入，在 `OnBind` 手写接线）
- `OnReady()`：可选；在生成的节点解析之后调用
- `OnBind(vm, bindings)`：可选；在场景绑定和声明式绑定之后调用，用于额外接线。`bindings` 是 `Telepath.Core.BindingSet`
- `_Notification`：只声明 partial 方法，不要自行实现或覆盖其他生命周期方法

薄 View 可以既没有 `[BindTo]` 也没有 `OnBind`（绑定全在场景上）。写了 `OnBind` 则签名必须是 `void OnBind(TViewModel vm, BindingSet bindings)`。

`BindingSet` 只收集一次进树周期的订阅。接线走 `Bind(source, BindingTarget)`（内部是 `OneWay` / `TwoWay`）、`BindCommand`、`BindItems`、`BindContent`、`BindOverlay` 和 `BindView`。Godot 只提供 Target 与命令适配；`Observable<T>` 一向，`BindableReactiveProperty<T>` 在目标支持 get/changed 时双向。

## `[BindTo]` 推断

按最具体控件类型选择方法。`CheckBox` / `OptionButton` 优先于 `BaseButton`。场景绑定的 `Kind = Auto` 用同一套规则。

| 控件 | 生成 | 默认方向 |
|------|------|----------|
| `Label` / `RichTextLabel` | `Bind(..., .Text())` | 一向；无 Converter 时 `ToStringConverter.Convert` |
| `LineEdit` / `TextEdit` | `Bind(..., .Text())` | `BindableReactiveProperty<string>` 双向，否则一向 |
| `CheckBox` / `CheckButton` | `Bind(..., .Toggle())` | 双向 `bool` |
| `OptionButton` | `Bind(..., .Selected())` | 双向 `long`；集合要写 `Kind = Items` |
| `ItemList` | `BindItems(..., .Items())` | 项集合；选中叠 `Kind = Selected`（双向 `long`，无选中为 `-1`） |
| 其他 `BaseButton` | `BindCommand` | 按下 `Execute`，`CanExecute` → `Disabled` |
| `Range`（Slider / SpinBox / ProgressBar / ScrollBar） | `Bind(..., .Value())` | `BindableReactiveProperty<double>` 双向，否则一向；`int` / `float` 隐式转 `double` |
| 带 `[TelepathView]` 的 `Control` | `BindView(..., .View())` | 注入父 VM 上的子 VM；`null` 隐藏 |
| 普通 `Control` | 报 TPV008 / 运行时抛错 | 必须设 `Kind` |

`Visible` / `Disabled` 不靠类型猜：

```csharp
[NodeInject("%Panel")]
[BindTo(nameof(Vm.ShowAdvanced), Kind = LinkKind.Visible)]
private Control _panel = null!;
```

- `.Visible()` → `CanvasItem.Visible`
- `.Disabled()` → `BaseButton.Disabled`（Godot 的 `Control` 没有 Disabled）
- `Kind` 也可覆盖推断，例如 CheckBox 当命令：`Kind = LinkKind.Command`
- 带参命令：按钮按下时从另一个控件取值。`LineEdit` + `Kind = Command` 则走 `TextSubmitted`，不必写 `Parameter`。场景绑定把 `parameter` 写成控件路径（`%Query`）
- 同一控件要绑多条（例如 `LineEdit` 既双向文本又回车提交）时，叠 `[BindTo]` 或在 Dock 里加多条场景绑定

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

Label / RichTextLabel 无 `Converter` 时用 `ToStringConverter.Convert`（与显式 `ToStringConverter<T>` 同一实现）。Range 的 `int` / `float` 走 `IntToDoubleConverter` / `FloatToDoubleConverter`。LineEdit / TextEdit 不隐式 `ToString()`，非 `string` 必须给转换器。

自定义转换实现 `Telepath.Core.IValueConverter<TSource, TTarget>`（一向）或 `ITwoWayValueConverter<TSource, TTarget>`（双向，再实现 `ConvertBack`）。类型必须具体、非开放泛型、有公共无参构造。命令绑定不能带 `Converter`。内置：`ToStringConverter<T>`、`IntToDoubleConverter`、`FloatToDoubleConverter`；隐式绑定走同一套实现。

场景绑定把 `converter` 写成完整类型名，例如 `Telepath.Showcase.CounterApp.CountTextConverter`。`[BindTo]` 写法：

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

## 列表

集合不走 `BindingTarget<T>`。`CollectionTarget<T>` 提供 `Reset` / `Insert` / `RemoveAt` / `Replace` / `Move`。`ObservableList<T>` 增量更新，`Observable<IReadOnlyList<T>>`（含 `BindableReactiveProperty`）每次整表 `Reset`。可选 `Func` / `IValueConverter` 把项转成目标类型。

Godot：`ItemList.Items()`、`OptionButton.Items()` 都是 `CollectionTarget<string>`。`ItemList` 默认绑集合；选中叠 `Kind = Selected`。`OptionButton` 默认仍是 `Selected`，集合要写 `Kind = Items`。容器模板必须 `Kind = Items`，并给 `item_view` + `item_scene`（场景绑定）或 `[BindTo]` 的 `ItemView` + `ItemScene`。出树时 `Detach` 会拆掉生成的子节点，不释放项 VM。

```csharp
[NodeInject("%Items")]
[BindTo(nameof(ListViewModel.Items))]
[BindTo(nameof(ListViewModel.Selected), Kind = LinkKind.Selected)]
private ItemList _items = null!;

[NodeInject("%Choices")]
[BindTo(nameof(ListViewModel.Items), Kind = LinkKind.Items)]
private OptionButton _choices = null!;
```

```csharp
[NodeInject("%Items")]
[BindTo(nameof(TodoListViewModel.Items), Kind = LinkKind.Items,
    ItemView = typeof(TodoItemView), ItemScene = nameof(ItemScene))]
private VBoxContainer _items = null!;
```

生成 `bindings.BindItems(vm.Items, _items.Items())` 和 `bindings.BindItems(vm.Items, _list.Items<TodoItemView, TodoItemViewModel>(ItemScene))`。原生列表可用 `Converter` 把项转成 `string`；容器模板不能带 `Converter`。

`[Bindable] private ObservableList<T>? _items` 生成惰性 `ObservableList<T> Items`，不要包成 `BindableReactiveProperty`。整表替换仍用 `BindableReactiveProperty<IReadOnlyList<T>>`。项若是 ViewModel，从集合移除和父 `OnDispose` 时由拥有者 `Dispose`。示例：[ListApp](../samples/Showcase/ListApp/)（`ItemList`），[TodoApp](../samples/Showcase/TodoApp/)（容器 + 子 View）。ViewModel 契约见 [viewmodel.md](viewmodel.md)，换屏见 [presentation.md](presentation.md)，R3 胶水见 [r3-godot.md](r3-godot.md)。
