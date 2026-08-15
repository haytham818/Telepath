# ViewModel

UI 状态与命令容器，拥有属性与命令，不知道 Godot 节点

```
src/Telepath.Core/ViewModel/
  IViewModel.cs
  ViewModel.cs
  BindableAttribute.cs
  CommandAttribute.cs
src/Telepath.SourceGenerator/ViewModel/
```

示例：[samples/Showcase/CounterApp](../samples/Showcase/CounterApp/CounterViewModel.cs)。

## API

- `Track(disposable)` 登记到内部 `DisposableBag`
- `Dispose`：幂等操作，先 `OnDispose()`，再释放 bag。已 Dispose 后再 `Track` 会抛出 `ObjectDisposedException`。
- 派生类清非 Track 资源时重写 `OnDispose`，不要重写 `Dispose`。

### 源生成

类必须是继承 `ViewModel` 的非泛型 `partial` 类。生成成员惰性初始化并 `Track`，构造函数不必调用 `Initialize()`。字段只作初值，构造里赋值后改用生成属性的 `.Value`。

| 声明 | 生成 |
|------|------|
| `[Bindable] private int _count` | `BindableReactiveProperty<int> Count` |
| `[Bindable(nameof(Count))] GetCountText(int count)` | `BindableReactiveProperty<string> CountText`（去掉 `Get` / `Compute` / `Format`） |
| `[Command] OnIncrement()` | `ReactiveCommand IncrementCommand`（去掉 `On`，再加 `Command` 后缀） |
| `[Command] OnSearch(string query)` | `ReactiveCommand<string> SearchCommand` |
| `[Command(CanExecute = nameof(CanIncrement))]` | `CanExecute` 必须是 `Observable<bool>` 属性或无参方法 |

可用 `Name = "..."` 覆盖生成名。多个 `From` 用 `Observable.CombineLatest`。

```csharp
public sealed partial class CounterViewModel : ViewModel
{
    [Bindable]
    private int _count;

    public CounterViewModel(int initial = 0)
    {
        _count = initial;
    }

    [Bindable(nameof(Count))]
    private string GetCountText(int count) => $"Count: {count}";

    [Command(CanExecute = nameof(CanIncrement))]
    private void OnIncrement() => Count.Value++;

    private Observable<bool> CanIncrement() => Count.Select(c => c < 10);
}
```

方法零个参数生成 `ReactiveCommand`，一个参数生成 `ReactiveCommand<T>`。两个及以上参数会报错。View 侧用 `[LinkTo(..., Parameter = nameof(_query))]` 在按钮按下时取值，见 [view.md](view.md)。

仍可手写 `BindableReactiveProperty` / `ReactiveCommand` / `ReactiveCommand<T>` 并 `Track`。

## 与 Godot 节点生命周期间的关系

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` / `_EnterTree` | 仅首次创建时 new；不要每次进树都 new | 接绑定 |
| `_ExitTree` | **不** `Dispose`（节点可能再进树） | 断绑定 |
| 真正释放（`NotificationPredelete` / `Free`） | `viewModel.Dispose()` | 在 `_ExitTree` 时已断 |

R3 的 `AddTo(Node)` 在出树时 Dispose，只适合绑定订阅，不要用来挂 ViewModel。View 生命周期见 [view.md](view.md)，时钟与胶水见 [r3-godot.md](r3-godot.md)。
