using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class PauseDemoViewModel : ViewModel, IActivatable
{
    private readonly IOverlayHost _overlay;
    private IDisposable? _tick;

    [Bindable]
    private int _ticks = 0;

    [Bindable]
    private string _status = "Paused";

    public PauseDemoViewModel(IOverlayHost overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    public void Activate()
    {
        if (_tick is not null)
        {
            return;
        }

        Status.Value = "Running";
        _tick = Observable.Interval(
                TimeSpan.FromMilliseconds(200),
                ObservableSystem.DefaultTimeProvider)
            .Subscribe(_ => Ticks.Value++);
    }

    public void Deactivate()
    {
        _tick?.Dispose();
        _tick = null;
        if (!IsDisposed)
        {
            Status.Value = "Paused";
        }
    }

    [Command]
    private void OnCover() => _overlay.Push<AboutViewModel>();

    [Command]
    private void OnCoverContinue() => _overlay.Push<AboutViewModel>(CoverMode.Continue);

    [Command]
    private void OnCoverModal() => _overlay.Push<AboutViewModel>(OverlayLayer.Modal);

    [Command]
    private void OnToast() => _overlay.Push<ToastViewModel>(OverlayLayer.Toast);

    protected override void OnDispose() => Deactivate();
}
