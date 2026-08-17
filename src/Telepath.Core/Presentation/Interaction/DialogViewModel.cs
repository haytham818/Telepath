namespace Telepath.Core;

/// <summary>
/// Overlay dialog that completes a <see cref="Task{TResult}"/>. Buttons call
/// <see cref="Complete"/>; they do not close the overlay. <see cref="IInteraction.Run{T}"/>
/// pushes the instance and closes it when the task completes. Back, Clear, and
/// dispose finish with <see cref="Dismissed"/>.
/// </summary>
public abstract class DialogViewModel<T> : ViewModel
{
    private readonly TaskCompletionSource<T> _completion = new();

    /// <summary>
    /// Completes when the dialog is answered or dismissed.
    /// </summary>
    public Task<T> Completion => _completion.Task;

    /// <summary>
    /// Result used when the dialog is closed without <see cref="Complete"/>.
    /// </summary>
    protected abstract T Dismissed { get; }

    /// <summary>
    /// Answers the dialog. The first call wins; later calls are ignored.
    /// </summary>
    protected void Complete(T result) => _completion.TrySetResult(result);

    /// <inheritdoc />
    protected override void OnDispose() => _completion.TrySetResult(Dismissed);
}
