namespace Telepath.Godot;

/// <summary>
/// Optional cover / uncover animation owned by a host view that stays bound
/// while another overlay is stacked above it. Distinct from
/// <see cref="IViewTransition"/> (this view's own enter / exit) and from
/// <see cref="Telepath.Core.IActivatable"/> (pause / resume of the ViewModel).
/// </summary>
/// <remarks>
/// Presenters call these when the overlay host's visual z-order changes,
/// including Continue toasts. <see cref="PlayCoverAsync"/> must apply the first
/// visual frame synchronously so the control does not flash uncovered.
/// Completing the task is not becoming the foreground item; the covering
/// overlay's enter plays in parallel.
/// </remarks>
public interface IViewCoverTransition
{
    /// <summary>
    /// Plays the covered transition. The view is still bound and may read its
    /// ViewModel. The covering overlay is entering in parallel.
    /// </summary>
    Task PlayCoverAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Plays the uncover transition after nothing remains stacked above this
    /// view. Skipped when the view is exiting or when overlay teardown
    /// <c>Clear(resumeCovered: false)</c> holds the screen covered until navigate.
    /// </summary>
    Task PlayUncoverAsync(CancellationToken cancellationToken);
}
