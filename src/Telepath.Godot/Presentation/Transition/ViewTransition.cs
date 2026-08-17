using Godot;

namespace Telepath.Godot;

internal static class ViewTransition
{
    public static Task PlayEnterAsync(Control view, CancellationToken cancellationToken)
    {
        if (view is IViewTransition transition)
        {
            return transition.PlayEnterAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public static Task PlayExitAsync(Control view, CancellationToken cancellationToken)
    {
        if (view is IViewTransition transition)
        {
            return transition.PlayExitAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }
}
