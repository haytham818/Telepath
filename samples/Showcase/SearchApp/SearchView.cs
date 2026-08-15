using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.SearchApp;

[TelepathView<SearchViewModel>]
public partial class SearchView : Control
{
    [LinkTo("%Query", nameof(SearchViewModel.Query))]
    [LinkTo("%Query", nameof(SearchViewModel.SearchCommand), Kind = LinkKind.Command)]
    private LineEdit _query = null!;

    [LinkTo("%Search", nameof(SearchViewModel.SearchCommand), Parameter = nameof(_query))]
    private Button _search = null!;

    [LinkTo("%Result", nameof(SearchViewModel.Result))]
    private Label _result = null!;

    public override partial void _Notification(int what);

    private SearchViewModel CreateViewModel() => new();
}
