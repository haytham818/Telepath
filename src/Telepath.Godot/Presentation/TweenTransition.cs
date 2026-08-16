using Godot;

namespace Telepath.Godot;

/// <summary>
/// Completes when a <see cref="Tween"/> finishes or the token is cancelled.
/// Host views that implement <see cref="IViewTransition"/> configure the tween
/// (including the first visual frame) and forward to this helper.
/// </summary>
public static class TweenTransition
{
    /// <summary>
    /// Waits until <paramref name="tween"/> emits <see cref="Tween.Finished"/>.
    /// Cancelling calls <see cref="Tween.Kill"/>. The tween must already have
    /// tweens appended and be running.
    /// </summary>
    /// <param name="tween">A running tween bound to the view node.</param>
    /// <param name="cancellationToken">Kills the tween when cancelled. Cancel on the Godot main thread.</param>
    public static Task PlayAsync(Tween tween, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tween);
        if (cancellationToken.IsCancellationRequested)
        {
            Kill(tween);
            return Task.FromCanceled(cancellationToken);
        }

        if (!tween.IsValid() || !tween.IsRunning())
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();

        void OnFinished() => tcs.TrySetResult();

        tween.Finished += OnFinished;
        var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        tcs.Task.ContinueWith(
            _ =>
            {
                registration.Dispose();
                if (!GodotObject.IsInstanceValid(tween))
                {
                    return;
                }

                tween.Finished -= OnFinished;
                if (cancellationToken.IsCancellationRequested)
                {
                    Kill(tween);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return tcs.Task;
    }

    private static void Kill(Tween tween)
    {
        if (GodotObject.IsInstanceValid(tween) && tween.IsValid())
        {
            tween.Kill();
        }
    }
}
