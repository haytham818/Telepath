namespace Telepath.Core;

/// <summary>
/// Presents <see cref="DialogViewModel{T}"/> instances through an
/// <see cref="IOverlayHost"/>. Owned by the shell; pages take
/// <see cref="IInteraction"/>.
/// </summary>
public sealed class Interaction : IInteraction
{
    private readonly IOverlayHost _overlay;

    public Interaction(IOverlayHost overlay)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        _overlay = overlay;
    }

    /// <inheritdoc />
    public async Task<T> Run<T>(
        DialogViewModel<T> dialog,
        OverlayLayer? layer = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        ObjectDisposedException.ThrowIf(dialog.IsDisposed, dialog);

        var target = layer ?? OverlayLayer.Modal;
        try
        {
            _overlay.Push(dialog, target);
            using var registration = cancellationToken.Register(
                () => TryClose(_overlay, dialog));
            return await dialog.Completion.ConfigureAwait(true);
        }
        finally
        {
            TryClose(_overlay, dialog);
        }
    }

    /// <inheritdoc />
    public Task<bool> Confirm(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        return Run(
            new ConfirmViewModel(title, message),
            OverlayLayer.Modal,
            cancellationToken);
    }

    private static void TryClose(IOverlayHost overlay, IViewModel dialog)
    {
        if (dialog.IsDisposed)
        {
            return;
        }

        try
        {
            overlay.Close(dialog);
        }
        catch (InvalidOperationException)
        {
            dialog.Dispose();
        }
    }
}
