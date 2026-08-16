using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<AboutViewModel>]
public partial class AboutView : Control
{
    public override partial void _Notification(int what);

    private AboutViewModel CreateViewModel() =>
        throw new InvalidOperationException("AboutView expects an injected ViewModel.");
}
