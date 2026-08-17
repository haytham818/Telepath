using Godot;

namespace Telepath.Godot;

/// <summary>
/// Plays a named clip on an <see cref="AnimationPlayer"/> until it finishes or
/// the token is cancelled. Host views that implement
/// <see cref="IViewTransition"/> can forward to this helper.
/// </summary>
public static class AnimationPlayerTransition
{
    /// <summary>
    /// Plays <paramref name="animation"/> and completes when
    /// <see cref="AnimationMixer.AnimationFinished"/> fires for that clip.
    /// </summary>
    /// <param name="player">Player that owns the clip. Must stay valid until the task completes.</param>
    /// <param name="animation">Non-looping clip name.</param>
    /// <param name="cancellationToken">Stops the player when cancelled. Cancel on the Godot main thread.</param>
    public static Task PlayAsync(
        AnimationPlayer player,
        StringName animation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (!player.HasAnimation(animation))
        {
            throw new InvalidOperationException(
                $"AnimationPlayer '{player.Name}' has no animation '{animation}'.");
        }

        var tcs = new TaskCompletionSource();

        void OnFinished(StringName name)
        {
            if (name == animation)
            {
                tcs.TrySetResult();
            }
        }

        player.AnimationFinished += OnFinished;
        var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        tcs.Task.ContinueWith(
            _ =>
            {
                registration.Dispose();
                if (!GodotObject.IsInstanceValid(player))
                {
                    return;
                }

                player.AnimationFinished -= OnFinished;
                if (cancellationToken.IsCancellationRequested)
                {
                    player.Stop();
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        player.Play(animation);
        return tcs.Task;
    }
}
