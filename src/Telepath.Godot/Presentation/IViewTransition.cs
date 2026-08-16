namespace Telepath.Godot;

/// <summary>
/// Optional enter / exit animation owned by a host view. Presenters call these
/// after the node is in the tree (enter) or after the ViewModel is cleared
/// (exit). Views that do not implement this interface switch instantly.
/// </summary>
/// <remarks>
/// <see cref="PlayEnterAsync"/> must apply the first visual frame synchronously
/// so the control does not flash at full opacity. <see cref="PlayExitAsync"/>
/// must not read the ViewModel: the conductor or overlay has already disposed
/// it. Completing the task on the Godot main thread keeps follow-up
/// <c>QueueFree</c> on that thread when the animation is synchronous; async
/// completions are marshalled by the presenter.
/// </remarks>
public interface IViewTransition
{
    /// <summary>
    /// Plays the enter transition after inject, <c>AddChild</c>, and bind.
    /// Overlaps <see cref="Telepath.Core.IActivatable.Activate"/>; finishing enter is not becoming
    /// the foreground item.
    /// </summary>
    Task PlayEnterAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Plays the exit transition on an unbound ghost node. The presenter frees
    /// the node when this task completes or when a skip-animation teardown
    /// cancels <paramref name="cancellationToken"/>.
    /// </summary>
    Task PlayExitAsync(CancellationToken cancellationToken);
}
