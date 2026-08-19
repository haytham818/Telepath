using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class AboutViewModel : ViewModel
{
    private readonly IOverlayHost _overlay;
    private readonly IViewModelFactory _pages;

    [Bindable]
    private string _message =
        "This overlay covers the page. Modal / Toast above it will dim this panel; Pause or Continue chooses whether the page Deactivates.";

    public AboutViewModel(IOverlayHost overlay, IViewModelFactory pages)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(pages);
        _overlay = overlay;
        _pages = pages;
    }

    [Command]
    private void OnClose() => _overlay.Close(this);

    [Command]
    private void OnCoverModal() =>
        _overlay.Push(_pages.Create<AboutViewModel>(), OverlayLayer.Modal);

    [Command]
    private void OnToast() =>
        _overlay.Push(_pages.Create<ToastViewModel>(), OverlayLayer.Toast);
}
