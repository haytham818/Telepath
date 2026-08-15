using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Showcase.TodoApp;

public sealed partial class TodoListViewModel : ViewModel
{
    [Bindable]
    private string _draft = "";

    public ObservableList<TodoItemViewModel> Items { get; } = new();

    public TodoListViewModel()
    {
        AddItem("Write list bindings");
        AddItem("Make coffee");
    }

    [Command(CanExecute = nameof(CanAdd))]
    private void OnAdd(string text)
    {
        AddItem(text.Trim());
        Draft.Value = "";
    }

    private Observable<bool> CanAdd() => Draft.Select(static query => !string.IsNullOrWhiteSpace(query));

    private void AddItem(string title) => Items.Add(new TodoItemViewModel(title, RemoveItem));

    private void RemoveItem(TodoItemViewModel item)
    {
        if (Items.Remove(item))
        {
            item.Dispose();
        }
    }

    protected override void OnDispose()
    {
        foreach (var item in Items)
        {
            item.Dispose();
        }

        Items.Clear();
    }
}
