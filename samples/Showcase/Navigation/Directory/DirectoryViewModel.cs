using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class DirectoryViewModel : ViewModel
{
    private readonly INavigator _navigator;
    private readonly IOverlayHost _overlay;
    private readonly IInteraction _interaction;

    public DirectoryViewModel(INavigator navigator, IOverlayHost overlay, IInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(interaction);
        _navigator = navigator;
        _overlay = overlay;
        _interaction = interaction;
    }

    [Command]
    private void OnOpenCounter() => _navigator.Navigate(new CounterViewModel());

    [Command]
    private void OnOpenSearch() => _navigator.Navigate(new SearchViewModel());

    [Command]
    private void OnOpenList() => _navigator.Navigate(new ListViewModel());

    [Command]
    private void OnOpenTodo() => _navigator.Navigate(new TodoListViewModel(_interaction));

    [Command]
    private void OnOpenForm() => _navigator.Navigate(new FormViewModel());

    [Command]
    private void OnOpenPauseDemo() => _navigator.Navigate(new PauseDemoViewModel(_overlay));

    [Command]
    private void OnOpenBanner() =>
        _overlay.Push(new BannerViewModel(_overlay), ShellViewModel.Banner);
}
