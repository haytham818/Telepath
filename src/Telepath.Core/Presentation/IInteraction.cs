namespace Telepath.Core;

/// <summary>
/// Request/response UI: a page awaits a result, the host presents a dialog.
/// Injected at construction, the same way a page receives <see cref="INavigator"/>.
/// Not a message bus.
/// </summary>
public interface IInteraction
{
    /// <summary>
    /// Pushes <paramref name="dialog"/> onto <paramref name="layer"/> (default
    /// <see cref="OverlayLayer.Modal"/>) and returns when it completes.
    /// Back, Clear, and <paramref name="cancellationToken"/> finish with
    /// <see cref="DialogViewModel{T}.Dismissed"/>; they do not throw
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<T> Run<T>(
        DialogViewModel<T> dialog,
        OverlayLayer? layer = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a yes/no confirm dialog. <see langword="false"/> when the user
    /// chooses No, presses Back, or the request is cancelled.
    /// </summary>
    Task<bool> Confirm(
        string title,
        string message,
        CancellationToken cancellationToken = default);
}
