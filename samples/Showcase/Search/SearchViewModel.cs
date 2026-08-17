using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class SearchViewModel : ViewModel
{
    private static readonly string[] Catalog =
    [
        "apple",
        "apricot",
        "banana",
        "blueberry",
        "cherry",
        "grape",
        "orange",
        "peach",
        "pear",
        "strawberry",
    ];

    [Bindable]
    private string _query = "";

    [Bindable]
    private string _result = "Type a query, then Search or press Enter.";

    [Bindable]
    private double _progress = 0;

    [Bindable]
    private bool _isSearching = false;

    [Command(CanExecute = nameof(CanSearch))]
    private async Task OnSearch(string query, CancellationToken cancellationToken)
    {
        IsSearching.Value = true;
        Progress.Value = 0;
        Result.Value = $"Searching for '{query}'...";
        try
        {
            const int steps = 12;
            var stepDelay = TimeSpan.FromMilliseconds(100);
            for (var step = 1; step <= steps; step++)
            {
                await Task.Delay(stepDelay, ObservableSystem.DefaultTimeProvider, cancellationToken);
                Progress.Value = step / (double)steps;
            }

            var hits = Catalog
                .Where(item => item.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Result.Value = hits.Length == 0
                ? $"No results for '{query}'."
                : string.Join(", ", hits);
        }
        finally
        {
            IsSearching.Value = false;
            Progress.Value = 0;
        }
    }

    private Observable<bool> CanSearch() => Query.Select(static q => !string.IsNullOrWhiteSpace(q));
}
