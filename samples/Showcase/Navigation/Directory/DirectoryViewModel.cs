using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class DirectoryViewModel : ViewModel
{
    private readonly INavigator _navigator;
    private readonly IOverlayHost _overlay;
    private readonly IViewModelFactory _pages;

    public DirectoryViewModel(INavigator navigator, IOverlayHost overlay, IViewModelFactory pages)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(pages);
        _navigator = navigator;
        _overlay = overlay;
        _pages = pages;
    }

    [Command]
    private void OnOpenCounter() => _navigator.Navigate(_pages.Create<CounterViewModel>());

    [Command]
    private void OnOpenSearch() => _navigator.Navigate(_pages.Create<SearchViewModel>());

    [Command]
    private void OnOpenList() => _navigator.Navigate(_pages.Create<ListViewModel>());

    [Command]
    private void OnOpenTodo() => _navigator.Navigate(_pages.Create<TodoListViewModel>());

    [Command]
    private void OnOpenForm() => _navigator.Navigate(_pages.Create<FormViewModel>());

    [Command]
    private void OnOpenPauseDemo() => _navigator.Navigate(_pages.Create<PauseDemoViewModel>());

    [Command]
    private void OnOpenBanner() =>
        _overlay.Push(_pages.Create<BannerViewModel>(), ShellViewModel.Banner);
}
