using R3;

namespace Telepath.Core;

/// <summary>
/// Owns a single-slot navigation stack. Presentation state lives on the ViewModel;
/// the host view only observes <see cref="ActiveItem"/>.
/// </summary>
public interface IConductor : INavigator
{
    /// <summary>
    /// The page currently shown. <see langword="null"/> when the slot is empty.
    /// </summary>
    BindableReactiveProperty<IViewModel?> ActiveItem { get; }

    /// <summary>
    /// Closes <paramref name="viewModel"/> if it is active or on the back stack.
    /// Active pages fall back to <see cref="INavigator.Back"/> when the stack is
    /// non-empty; otherwise the slot is cleared. Pages that leave are disposed.
    /// </summary>
    void Close(IViewModel viewModel);
}
