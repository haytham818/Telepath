# ViewModel

UI 状态与命令容器，拥有属性与命令，不知道 Godot 节点

```
addons/Telepath/Core/ViewModel/
  IViewModel.cs
  ViewModel.cs
```


示例：[Showcase/CounterApp](../Showcase/CounterApp/CounterViewModel.cs)。

## API


- `Track(disposable)` 登记到内部 `DisposableBag`
- `Dispose`：幂等操作，先 `OnDispose()`，再释放 bag。已 Dispose 后再 `Track` 会抛出 `ObjectDisposedException`。
- 派生类清非 Track 资源时重写 `OnDispose`，不要重写 `Dispose`。

## 与 Godot 节点生命周期间的关系

| 节点生命周期 | ViewModel | 绑定 |
|----------|-----------|------|
| `_Ready` / `_EnterTree` | 仅首次创建时 new；不要每次进树都 new | 接绑定 |
| `_ExitTree` | **不** `Dispose`（节点可能再进树） | 断绑定 |
| 真正释放（`NotificationPredelete` / `Free`） | `viewModel.Dispose()` | 在 `_ExitTree` 时已断 |
