using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed partial class AboutViewModel : ViewModel
{
    private readonly IOverlay _overlay;

    [Bindable]
    private string _message =
        "This overlay covers the page. The view stays; Pause or Continue chooses whether it Deactivates.";

    public AboutViewModel(IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    [Command]
    private void OnClose() => _overlay.CloseSelf();
}
