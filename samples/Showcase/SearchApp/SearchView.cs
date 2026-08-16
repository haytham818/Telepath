using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.SearchApp;

[TelepathView<SearchViewModel>]
public partial class SearchView : Control
{
    [NodeInject("%Query")]
    [BindTo(nameof(SearchViewModel.Query))]
    [BindTo(nameof(SearchViewModel.SearchCommand), Kind = LinkKind.Command)]
    private LineEdit _query = null!;

    [NodeInject("%Search")]
    [BindTo(nameof(SearchViewModel.SearchCommand), Parameter = nameof(_query))]
    private Button _search = null!;

    [NodeInject("%Result")]
    [BindTo(nameof(SearchViewModel.Result))]
    private Label _result = null!;

    public override partial void _Notification(int what);

    private SearchViewModel CreateViewModel() => new();
}
