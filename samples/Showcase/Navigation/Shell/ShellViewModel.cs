using Godot;
using QFramework;
using R3;
using Telepath.Core;
using Telepath.Godot;

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

    public IViewModelFactory Pages { get; }

    public StatusViewModel Status { get; }

    public ShellViewModel()
    {
        Overlay = Track(new OverlayHost(() => ActiveItem.Value));
        Overlay.Register(Banner);
        Interaction = new Interaction(Overlay);
        Status = Track(new StatusViewModel());
        Track(Overlay.HasBackableOverlay.Subscribe(_ => UpdateCanGoBack()));
        Track(ActiveItem.Subscribe(UpdateStatus));

        ShowcaseApp.BindShell(this, Overlay, Interaction);
        Pages = ShowcaseApp.Interface.GetUtility<QfViewModelFactory>()
            ?? throw new InvalidOperationException("QfViewModelFactory was not registered.");
        Navigate(Pages.Create<DirectoryViewModel>());
    }

    public void BindPresentation(BindingSet bindings, Control content, Control overlay)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(overlay);

        var registry = CreateRegistry();
        var presented = new PresentedViews();
        bindings.BindContent(ActiveItem, content.Content(registry, presented));
        bindings.BindOverlayHost(Overlay, overlay, registry, presented);
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

    private static ViewRegistry CreateRegistry() => new ViewRegistry()
        .Register<DirectoryViewModel>("res://Navigation/Directory/DirectoryView.tscn")
        .Register<CounterViewModel>("res://Counter/CounterView.tscn")
        .Register<SearchViewModel>("res://Search/SearchView.tscn")
        .Register<ListViewModel>("res://List/ListView.tscn")
        .Register<TodoListViewModel>("res://Todo/TodoListView.tscn")
        .Register<FormViewModel>("res://Form/FormView.tscn")
        .Register<PauseDemoViewModel>("res://Navigation/Pause/PauseDemoView.tscn")
        .Register<AboutViewModel>("res://Navigation/About/AboutView.tscn")
        .Register<ToastViewModel>("res://Navigation/Toast/ToastView.tscn")
        .Register<BannerViewModel>("res://Navigation/Banner/BannerView.tscn")
        .Register<ConfirmViewModel>("res://Navigation/Confirm/ConfirmView.tscn");
}
