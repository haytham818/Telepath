using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.TodoApp;

[TelepathView<TodoListViewModel>]
public partial class TodoListView : Control
{
    public override partial void _Notification(int what);

    private TodoListViewModel CreateViewModel() =>
        throw new InvalidOperationException("TodoListView expects an injected ViewModel.");
}
