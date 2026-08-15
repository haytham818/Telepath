using R3;
using Telepath.Core;

namespace Telepath.Showcase.SearchApp;

public sealed partial class SearchViewModel : ViewModel
{
    [Bindable]
    private string _query = "";

    [Bindable]
    private string _result = "Type a query, then Search or press Enter.";

    [Command(CanExecute = nameof(CanSearch))]
    private void OnSearch(string query)
    {
        Result.Value = $"Last search: {query}";
    }

    private Observable<bool> CanSearch() => Query.Select(static q => !string.IsNullOrWhiteSpace(q));
}
