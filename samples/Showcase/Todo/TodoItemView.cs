using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

// Attribute bindings remain valid as an escape hatch; the list scene uses the editor binder.
[TelepathView<TodoItemViewModel>]
public partial class TodoItemView : HBoxContainer
{
    [NodeInject("%Done")]
    [BindTo(nameof(TodoItemViewModel.Done))]
    private CheckBox _done = null!;

    [NodeInject("%Title")]
    [BindTo(nameof(TodoItemViewModel.Title))]
    private Label _title = null!;

    [NodeInject("%Remove")]
    [BindTo(nameof(TodoItemViewModel.RemoveCommand))]
    private Button _remove = null!;

    public override partial void _Notification(int what);

    private TodoItemViewModel CreateViewModel() =>
        throw new InvalidOperationException("TodoItemView expects an injected ViewModel.");
}
