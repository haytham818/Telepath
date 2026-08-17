using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class TodoItemViewModel : ViewModel
{
    private readonly Func<TodoItemViewModel, Task> _remove;

    [Bindable]
    private string _title;

    [Bindable]
    private bool _done;

    public TodoItemViewModel(string title, Func<TodoItemViewModel, Task> remove)
    {
        _title = title;
        _done = false;
        _remove = remove;
    }

    [Command]
    private Task OnRemove() => _remove(this);
}
