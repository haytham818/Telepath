using Godot;

namespace Telepath.Godot;

internal static class ViewCoverTransition
{
    public static Task PlayCoverAsync(Control view, CancellationToken cancellationToken)
    {
        if (view is IViewCoverTransition transition)
        {
            return transition.PlayCoverAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public static Task PlayUncoverAsync(Control view, CancellationToken cancellationToken)
    {
        if (view is IViewCoverTransition transition)
        {
            return transition.PlayUncoverAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }
}
