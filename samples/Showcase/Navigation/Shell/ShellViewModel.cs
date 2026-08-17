using R3;
using Telepath.Core;

namespace Telepath.Showcase;

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

    public StatusViewModel Status { get; }

    public ShellViewModel()
    {
        Overlay = Track(new OverlayHost(() => ActiveItem.Value));
        Overlay.Register(Banner);
        Interaction = new Interaction(Overlay);
        Status = Track(new StatusViewModel());
        Track(Overlay.HasBackableOverlay.Subscribe(_ => UpdateCanGoBack()));
        Track(ActiveItem.Subscribe(UpdateStatus));
    }

    public override bool Back() => Overlay.Back() || base.Back();

    public override void Navigate(IViewModel viewModel)
    {
        Overlay.Clear(resumeCovered: false);
        base.Navigate(viewModel);
    }

    protected override bool ComputeCanGoBack()
        => Overlay.HasBackableOverlay.Value || base.ComputeCanGoBack();

    private void UpdateStatus(IViewModel? item)
    {
        Status.Caption.Value = item is null
            ? "Showcase"
            : item.GetType().Name.Replace("ViewModel", "", StringComparison.Ordinal);
    }
}
