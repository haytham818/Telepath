using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed partial class ToastViewModel : ViewModel
{
    private readonly IOverlay _overlay;

    [Bindable]
    private string _message = "Toast is above Modal. Shell Back skips it.";

    public ToastViewModel(IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    [Command]
    private void OnClose() => _overlay.Close(this);
}
