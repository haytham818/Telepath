using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<SearchViewModel>]
public partial class SearchView : Control
{
    public override partial void _Notification(int what);

    private SearchViewModel CreateViewModel() => new();
}
