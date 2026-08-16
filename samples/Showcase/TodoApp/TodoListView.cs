using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.TodoApp;

[TelepathView<TodoListViewModel>]
public partial class TodoListView : Control
{
    public override partial void _Notification(int what);

    private TodoListViewModel CreateViewModel() => new();
}
