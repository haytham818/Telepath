using R3;
using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed class ShellViewModel : Conductor
{
    public static OverlayLayer Banner { get; } = new(
        "Banner",
        order: 50,
        handlesBack: false,
        defaultCover: CoverMode.Continue,
        blocksPassThrough: false);

    public OverlayHost Overlay { get; }

    public IInteraction Interaction { get; }

    public ShellViewModel()
    {
        Overlay = Track(new OverlayHost(() => ActiveItem.Value));
        Overlay.Register(Banner);
        Interaction = new Interaction(Overlay);
        Track(Overlay.HasBackableOverlay.Subscribe(_ => UpdateCanGoBack()));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack()
        => Overlay.HasBackableOverlay.Value || base.ComputeCanGoBack();
}
