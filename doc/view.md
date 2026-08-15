# View

Godot `Control` 上的 View 基类：管绑定寿命，不管 ViewModel 内部状态。

```
addons/Telepath/Godot/View/View.cs          View / View<TViewModel>
addons/Telepath/Godot/Binding/BindingSet.cs BindLabel / BindCommand / Add
```

示例：[Showcase/CounterApp/CounterView.cs](../Showcase/CounterApp/CounterView.cs)。

脚本类必须是**非泛型**（`CounterView : View<CounterViewModel>`）。`View` / `View<T>` 在 `Telepath.Godot`；具体 View 脚本由宿主编译。

Godot 的 C# 源生成器只登记**当前脚本类自己声明的** `_Ready` 等方法。`Telepath.Godot` 不引用 `Godot.SourceGenerators`。`View` 的方法表写在同一文件里；`View/` 目录有 `.gdignore`，避免编辑器把库基类当成脚本重复注册（`ScriptTypeBiMap` 重复键）。

## 寿命

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` | 若未注入则 `CreateViewModel()` 一次 | `OnReady` 取节点后 `OnBind` |
| `_EnterTree`（已 Ready、无绑定） | 不 new | 再次 `OnBind` |
| `_ExitTree` | **不** `Dispose` | `BindingSet.Dispose` |
| `NotificationPredelete` | `ViewModel.Dispose()` | 已在出树时断开 |

不要 `viewModel.AddTo(node)`。订阅进 `BindingSet`，或 `bindings.Add(...)`。

## API

- `ViewModel`：可注入；`_Ready` 时仍为空才 `CreateViewModel()`
- `OnReady()`：`GetNode`；不要 override `_Ready` 而不调 `base`
- `OnBind(vm, bindings)`：只接线

```csharp
public partial class CounterView : View<CounterViewModel>
{
    private Label _countLabel = null!;
    private Button _incrementButton = null!;

    protected override void OnReady()
    {
        _countLabel = GetNode<Label>("%CountLabel");
        _incrementButton = GetNode<Button>("%IncrementButton");
    }

    protected override CounterViewModel CreateViewModel() => new();

    protected override void OnBind(CounterViewModel vm, BindingSet bindings)
    {
        bindings.BindLabel(vm.CountText, _countLabel);
        bindings.BindCommand(vm.Increment, _incrementButton);
    }
}
```

`BindLabel` → `SubscribeToLabel`。`BindCommand`：按下 `Execute(Unit)`，`CanExecute` 同步 `Disabled`。

尚未纳入：声明式绑定、双向 Text、`ReactiveCommand<T>`、转换器、列表。ViewModel 契约见 [viewmodel.md](viewmodel.md)，R3 胶水见 [r3-godot.md](r3-godot.md)。
