using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<ToastViewModel>]
public partial class ToastView : Control
{
    public override partial void _Notification(int what);

    private ToastViewModel CreateViewModel() =>
        throw new InvalidOperationException("ToastView expects an injected ViewModel.");
}
