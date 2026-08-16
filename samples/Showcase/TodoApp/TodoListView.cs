using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.TodoApp;

[TelepathView<TodoListViewModel>]
public partial class TodoListView : Control
{
    [Export]
    public PackedScene ItemScene { get; set; } = null!;

    [NodeInject("%Draft")]
    [BindTo(nameof(TodoListViewModel.Draft))]
    [BindTo(nameof(TodoListViewModel.AddCommand), Kind = LinkKind.Command)]
    private LineEdit _draft = null!;

    [NodeInject("%Add")]
    [BindTo(nameof(TodoListViewModel.AddCommand), Parameter = nameof(_draft))]
    private Button _add = null!;

    [NodeInject("%Items")]
    [BindTo(nameof(TodoListViewModel.Items), Kind = LinkKind.Items,
        ItemView = typeof(TodoItemView), ItemScene = nameof(ItemScene))]
    private VBoxContainer _items = null!;

    public override partial void _Notification(int what);

    private TodoListViewModel CreateViewModel() => new();

    private void OnReady()
    {
        ItemScene ??= GD.Load<PackedScene>("res://TodoApp/TodoItemView.tscn");
    }
}
