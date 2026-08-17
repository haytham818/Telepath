using Godot;

namespace Telepath.Godot;

internal static class ViewTransitionPlayback
{
    public static void Play(
        Control view,
        CancellationToken cancellationToken,
        Func<Control, CancellationToken, Task> play,
        Action<Control> onCompleted)
    {
        Task task;
        try
        {
            task = play(view, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            onCompleted(view);
            return;
        }

        if (task.IsCompleted)
        {
            Observe(task);
            if (!cancellationToken.IsCancellationRequested)
            {
                onCompleted(view);
            }

            return;
        }

        ContinueAsync(view, task, cancellationToken, onCompleted);
    }

    private static async void ContinueAsync(
        Control view,
        Task task,
        CancellationToken cancellationToken,
        Action<Control> onCompleted)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Callable.From(() =>
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                onCompleted(view);
            }
        }).CallDeferred();
    }

    private static void Observe(Task task)
    {
        if (!task.IsFaulted)
        {
            return;
        }

        GD.PushError(task.Exception!.GetBaseException().ToString());
    }
}
