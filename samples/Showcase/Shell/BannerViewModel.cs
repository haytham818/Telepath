using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed partial class BannerViewModel : ViewModel
{
    private readonly IOverlay _overlay;

    [Bindable]
    private string _message = "Custom Banner band (order 50), between Popup and Modal.";

    public BannerViewModel(IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    [Command]
    private void OnClose() => _overlay.Close(this);
}
