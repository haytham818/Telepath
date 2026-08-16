using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed partial class AboutViewModel : ViewModel
{
    private readonly IOverlay _overlay;

    [Bindable]
    private string _message =
        "This overlay covers the page. Bindings stay; the page Deactivates until you close.";

    public AboutViewModel(IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    [Command]
    private void OnClose() => _overlay.CloseSelf();
}
