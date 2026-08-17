using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class ListViewModel : ViewModel
{
    [Bindable]
    private string _draft = "";

    [Bindable]
    private long _selected = -1;

    [Bindable]
    private ObservableList<string>? _items;

    public ListViewModel()
    {
        Items.Add("Alpha");
        Items.Add("Beta");
        Items.Add("Gamma");
    }

    [Command(CanExecute = nameof(CanAdd))]
    private void OnAdd(string text)
    {
        Items.Add(text.Trim());
        Draft.Value = "";
    }

    private Observable<bool> CanAdd() => Draft.Select(static query => !string.IsNullOrWhiteSpace(query));

    [Command(CanExecute = nameof(CanRemove))]
    private void OnRemove()
    {
        var index = (int)Selected.Value;
        if ((uint)index >= (uint)Items.Count)
        {
            return;
        }

        Items.RemoveAt(index);
        Selected.Value = Items.Count == 0 ? -1 : Math.Min(index, Items.Count - 1);
    }

    private Observable<bool> CanRemove() => Selected.Select(static index => index >= 0);

    [Command]
    private void OnClear()
    {
        Items.Clear();
        Selected.Value = -1;
    }
}
