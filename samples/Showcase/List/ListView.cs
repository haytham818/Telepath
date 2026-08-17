using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<ListViewModel>]
public partial class ListView : Control
{
    public override partial void _Notification(int what);

    private ListViewModel CreateViewModel() => new();
}
