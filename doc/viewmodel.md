# ViewModel

UI 状态与命令容器，拥有属性与命令，不知道 Godot 节点

```
src/Telepath.Core/ViewModel/
  IViewModel.cs
  ViewModel.cs
  ViewModel.Commands.cs
  BindableAttribute.cs
  CommandAttribute.cs
src/Telepath.Core/Presentation/
  IActivatable.cs、Conductor/（Conductor、INavigator）
src/Telepath.SourceGenerator/ViewModel/
```

示例：[samples/Showcase/CounterApp](../samples/Showcase/CounterApp/CounterViewModel.cs)。

## API

- `Track(disposable)` 登记到内部 `DisposableBag`
- `Command` / `AsyncCommand`：创建并 `Track` 命令；已 Dispose 后再调用会抛出 `ObjectDisposedException`
- `Dispose`：幂等操作，先 `OnDispose()`，再释放 bag。已 Dispose 后再 `Track` 会抛出 `ObjectDisposedException`。
- 派生类清非 Track 资源时重写 `OnDispose`，不要重写 `Dispose`。

### 源生成

类必须是继承 `ViewModel` 的非泛型 `partial` 类。生成成员惰性初始化并 `Track`，构造函数不必调用 `Initialize()`。字段只作初值，构造里赋值后改用生成属性的 `.Value`。

| 声明 | 生成 |
|------|------|
| `[Bindable] private int _count` | `BindableReactiveProperty<int> Count` |
| `[Bindable] private ObservableList<string>? _items` | `ObservableList<string> Items`（惰性 `??= new()`，不 Track） |
| `[Bindable(nameof(Count), nameof(Max))] GetIsAtMax(int count, int max)` | `BindableReactiveProperty<bool> IsAtMax`（去掉 `Get` / `Compute` / `Format`） |
| `[Command] OnIncrement()` | `ReactiveCommand IncrementCommand`（去掉 `On`，再加 `Command` 后缀），走 `Command` |
| `[Command] OnSearch(string query)` | `ReactiveCommand<string> SearchCommand`，走 `Command<T>` |
| `[Command] async Task OnSearch(string query, CancellationToken ct)` | 同样生成 `ReactiveCommand<string>`，走 `AsyncCommand` |
| `[Command(CanExecute = nameof(CanIncrement))]` | `CanExecute` 必须是 `Observable<bool>` 属性或无参方法 |

可用 `Name = "..."` 覆盖生成名。多个 `From` 用 `Observable.CombineLatest`。

```csharp
public sealed partial class CounterViewModel : ViewModel
{
    [Bindable]
    private int _count;

    [Bindable]
    private int _max = 10;

    public CounterViewModel(int initial = 0)
    {
        _count = initial;
    }

    [Bindable(nameof(Count), nameof(Max))]
    private bool GetIsAtMax(int count, int max) => count >= max;

    [Command(CanExecute = nameof(CanIncrement))]
    private void OnIncrement() => Count.Value++;

    private Observable<bool> CanIncrement() => Count.Select(c => c < 10);
}
```

UI 格式化（例如把 `Count` 显示成 `"Count: 3"`）不要做成派生 bindable，用场景绑定的 `converter` 或 View 侧 `[BindTo(..., Converter = typeof(...))]`，见 [view.md](view.md)。

方法零个参数生成 `ReactiveCommand`，一个参数生成 `ReactiveCommand<T>`。返回值可以是 `void`、`Task` 或 `ValueTask`；异步方法还可以把 `CancellationToken` 放在最后。`async void`、`Task<T>` / `ValueTask<T>`、两个及以上业务参数会报错。View 侧用 `[BindTo(..., Parameter = nameof(_query))]` 在按钮按下时取值，见 [view.md](view.md)。

生成器和手写都走 `ViewModel.Command` / `AsyncCommand`，内部 `Track`。异步命令执行期间 `CanExecute` 为 false（按钮会禁用），重叠触发默认 Drop，`Dispose` 会取消传入的 `CancellationToken`。延时请用 `ObservableSystem.DefaultTimeProvider`（Godot 下是帧时钟），避免 `Task.Delay` 完成后在线程池上改绑定属性。

```csharp
[Command(CanExecute = nameof(CanSearch))]
private async Task OnSearch(string query, CancellationToken cancellationToken)
{
    Result.Value = $"Searching for '{query}'...";
    await Task.Delay(TimeSpan.FromMilliseconds(400), ObservableSystem.DefaultTimeProvider, cancellationToken);
    Result.Value = $"Last search: {query}";
}
```

仍可手写 `BindableReactiveProperty` 并 `Track`，或手写 `Command(...)` / `AsyncCommand(...)`。

`[Bindable]` 遇到 `ObservableList<T>` 时生成同类型惰性属性，不包进 `BindableReactiveProperty`（列表自己会通知）。字段不能是 `readonly`，不能带 `From`。整表替换（搜索结果、过滤）仍用 `BindableReactiveProperty<IReadOnlyList<T>>`。View 侧 `BindItems` / `[BindTo] Kind = Items` 见 [view.md](view.md)。

## 与 Godot 节点生命周期间的关系

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` / `_EnterTree` | 仅首次创建时 new；不要每次进树都 new | 接绑定 |
| `_ExitTree` | **不** `Dispose`（节点可能再进树） | 断绑定 |
| 真正释放（`NotificationPredelete` / `Free`） | 自己创建的才 `Dispose`；注入的项 VM 由集合拥有者释放 | 在 `_ExitTree` 时已断 |

R3 的 `AddTo(Node)` 在出树时 Dispose，只适合绑定订阅，不要用来挂 ViewModel。这是**资源寿命**，不等于打开 / 关闭 / 暂停：导航由 `Conductor` 调用可选的 `IActivatable`，离栈才 `Dispose` 页 VM，见 [presentation.md](presentation.md)。View 生命周期见 [view.md](view.md)，时钟与胶水见 [r3-godot.md](r3-godot.md)。
