using Telepath.Core;
using Telepath.Showcase.CounterApp;
using Telepath.Showcase.ListApp;
using Telepath.Showcase.SearchApp;
using Telepath.Showcase.TodoApp;

namespace Telepath.Showcase.Shell;

public sealed partial class DirectoryViewModel : ViewModel
{
    private readonly INavigator _navigator;
    private readonly IOverlay _overlay;

    public DirectoryViewModel(INavigator navigator, IOverlay overlay)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        _navigator = navigator;
        _overlay = overlay;
    }

    [Command]
    private void OnOpenCounter() => _navigator.Navigate(new CounterViewModel());

    [Command]
    private void OnOpenSearch() => _navigator.Navigate(new SearchViewModel());

    [Command]
    private void OnOpenList() => _navigator.Navigate(new ListViewModel());

    [Command]
    private void OnOpenTodo() => _navigator.Navigate(new TodoListViewModel());

    [Command]
    private void OnOpenPauseDemo() => _navigator.Navigate(new PauseDemoViewModel(_overlay));
}
