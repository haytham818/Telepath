using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase.SearchApp;

[TelepathView<SearchViewModel>]
public partial class SearchView : Control
{
    [LinkTo("%Query", nameof(SearchViewModel.Query))]
    private LineEdit _query = null!;

    [LinkTo("%Search", nameof(SearchViewModel.Search), Parameter = nameof(_query))]
    private Button _search = null!;

    [LinkTo("%Result", nameof(SearchViewModel.Result))]
    private Label _result = null!;

    public override partial void _Notification(int what);

    private SearchViewModel CreateViewModel() => new();

    private void OnBind(SearchViewModel vm, BindingSet bindings)
    {
        bindings.BindCommand(vm.Search, _query);
    }
}
