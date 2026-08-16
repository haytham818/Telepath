using R3;
using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed class ShellViewModel : Conductor
{
    public Overlay Overlay { get; }

    public ShellViewModel()
    {
        Overlay = Track(new Overlay(() => ActiveItem.Value));
        Track(Overlay.HasOverlay.Subscribe(_ => UpdateCanGoBack()));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack()
        => Overlay.HasOverlay.Value || base.ComputeCanGoBack();
}
