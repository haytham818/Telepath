namespace Telepath.Core;

/// <summary>
/// Navigation requests a child ViewModel can make without knowing the host conductor.
/// Injected at construction, the same way a list item receives a remove callback.
/// </summary>
public interface INavigator
{
    /// <summary>
    /// Pushes the current page (if any) and makes <paramref name="viewModel"/> active.
    /// The conductor takes ownership and will dispose the instance when it leaves the stack.
    /// </summary>
    void Navigate(IViewModel viewModel);

    /// <summary>
    /// Pops the current page and restores the previous one.
    /// Returns <see langword="false"/> when the stack is empty.
    /// </summary>
    bool Back();

    /// <summary>
    /// Closes the active page. No-op when nothing is active.
    /// </summary>
    void CloseSelf();
}
