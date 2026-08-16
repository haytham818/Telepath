using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<StatusViewModel>]
public partial class StatusView : Control
{
    public override partial void _Notification(int what);

    private StatusViewModel CreateViewModel() => new();
}
