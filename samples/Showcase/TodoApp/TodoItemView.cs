using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.TodoApp;

[TelepathView<TodoItemViewModel>]
public partial class TodoItemView : HBoxContainer
{
    [LinkTo("%Done", nameof(TodoItemViewModel.Done))]
    private CheckBox _done = null!;

    [LinkTo("%Title", nameof(TodoItemViewModel.Title))]
    private Label _title = null!;

    [LinkTo("%Remove", nameof(TodoItemViewModel.RemoveCommand))]
    private Button _remove = null!;

    public override partial void _Notification(int what);

    private TodoItemViewModel CreateViewModel() =>
        throw new InvalidOperationException("TodoItemView expects an injected ViewModel.");
}
