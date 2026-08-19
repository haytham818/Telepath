using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class AboutViewModel : ViewModel
{
    private readonly IOverlayHost _overlay;

    [Bindable]
    private string _message =
        "This overlay covers the page. Modal / Toast above it will dim this panel; Pause or Continue chooses whether the page Deactivates.";

    public AboutViewModel(IOverlayHost overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    [Command]
    private void OnClose() => _overlay.Close(this);

    [Command]
    private void OnCoverModal() => _overlay.Push<AboutViewModel>(OverlayLayer.Modal);

    [Command]
    private void OnToast() => _overlay.Push<ToastViewModel>(OverlayLayer.Toast);
}
