using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase.TodoApp;

[TelepathView<TodoListViewModel>]
public partial class TodoListView : Control
{
    [Export]
    public PackedScene ItemScene { get; set; } = null!;

    [LinkTo("%Draft", nameof(TodoListViewModel.Draft))]
    [LinkTo("%Draft", nameof(TodoListViewModel.AddCommand), Kind = LinkKind.Command)]
    private LineEdit _draft = null!;

    [LinkTo("%Add", nameof(TodoListViewModel.AddCommand), Parameter = nameof(_draft))]
    private Button _add = null!;

    private VBoxContainer _items = null!;

    public override partial void _Notification(int what);

    private TodoListViewModel CreateViewModel() => new();

    private void OnReady()
    {
        _items = GetNode<VBoxContainer>("%Items");
        ItemScene ??= GD.Load<PackedScene>("res://TodoApp/TodoItemView.tscn");
    }

    private void OnBind(TodoListViewModel vm, BindingSet bindings)
    {
        bindings.BindItems(vm.Items, _items.Items<TodoItemView, TodoItemViewModel>(ItemScene));
    }
}
