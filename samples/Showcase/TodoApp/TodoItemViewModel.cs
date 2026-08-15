using Telepath.Core;

namespace Telepath.Showcase.TodoApp;

public sealed partial class TodoItemViewModel : ViewModel
{
    private readonly Action<TodoItemViewModel> _remove;

    [Bindable]
    private string _title;

    [Bindable]
    private bool _done;

    public TodoItemViewModel(string title, Action<TodoItemViewModel> remove)
    {
        _title = title;
        _done = false;
        _remove = remove;
    }

    [Command]
    private void OnRemove() => _remove(this);
}
