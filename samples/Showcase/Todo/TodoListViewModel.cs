using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class TodoListViewModel : ViewModel
{
    private readonly IInteraction _interaction;

    [Bindable]
    private string _draft = "";

    [Bindable]
    private ObservableList<TodoItemViewModel>? _items;

    public TodoListViewModel(IInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction);
        _interaction = interaction;
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

    private async Task RemoveItem(TodoItemViewModel item)
    {
        if (!await _interaction.Confirm("Delete", $"Delete '{item.Title.Value}'?"))
        {
            return;
        }

        if (IsDisposed)
        {
            return;
        }

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
