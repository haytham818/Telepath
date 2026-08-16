using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Stack of overlay ViewModels shown on top of the current screen.
/// Covered views stay bound; only the top overlay is in the foreground.
/// </summary>
public interface IOverlay
{
    /// <summary>
    /// Overlay layers from bottom to top.
    /// </summary>
    ObservableList<IViewModel> Layers { get; }

    /// <summary>
    /// <see langword="true"/> when at least one overlay is open.
    /// </summary>
    BindableReactiveProperty<bool> HasOverlay { get; }

    /// <summary>
    /// Pushes <paramref name="viewModel"/> as the new top overlay.
    /// The overlay takes ownership and will dispose the instance when it leaves the stack.
    /// </summary>
    void Push(IViewModel viewModel);

    /// <summary>
    /// Pops the top overlay. Returns <see langword="false"/> when the stack is empty.
    /// </summary>
    bool Back();

    /// <summary>
    /// Closes <paramref name="viewModel"/> if it is on the overlay stack.
    /// The top overlay falls back to <see cref="Back"/>; lower layers are removed in place.
    /// </summary>
    void Close(IViewModel viewModel);

    /// <summary>
    /// Closes the top overlay. No-op when the stack is empty.
    /// </summary>
    void CloseSelf();

    /// <summary>
    /// Closes every overlay. When <paramref name="resumeCovered"/> is
    /// <see langword="true"/>, the covered screen is activated again.
    /// </summary>
    void Clear(bool resumeCovered = true);
}
