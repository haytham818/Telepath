using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<BannerViewModel>]
public partial class BannerView : Control
{
    public override partial void _Notification(int what);

    private BannerViewModel CreateViewModel() =>
        throw new InvalidOperationException("BannerView expects an injected ViewModel.");
}
