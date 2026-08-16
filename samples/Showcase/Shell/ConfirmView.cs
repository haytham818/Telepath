using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<ConfirmViewModel>]
public partial class ConfirmView : Control
{
    public override partial void _Notification(int what);

    private ConfirmViewModel CreateViewModel() =>
        throw new InvalidOperationException("ConfirmView expects an injected ViewModel.");
}
