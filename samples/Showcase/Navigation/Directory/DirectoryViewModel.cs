using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class DirectoryViewModel : ViewModel
{
    private readonly INavigator _navigator;
    private readonly IOverlayHost _overlay;

    public DirectoryViewModel(INavigator navigator, IOverlayHost overlay)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        _navigator = navigator;
        _overlay = overlay;
    }

    [Command]
    private void OnOpenCounter() => _navigator.Navigate<CounterViewModel>();

    [Command]
    private void OnOpenSearch() => _navigator.Navigate<SearchViewModel>();

    [Command]
    private void OnOpenList() => _navigator.Navigate<ListViewModel>();

    [Command]
    private void OnOpenTodo() => _navigator.Navigate<TodoListViewModel>();

    [Command]
    private void OnOpenForm() => _navigator.Navigate<FormViewModel>();

    [Command]
    private void OnOpenPauseDemo() => _navigator.Navigate<PauseDemoViewModel>();

    [Command]
    private void OnOpenBanner() => _overlay.Push<BannerViewModel>(ShellViewModel.Banner);
}
