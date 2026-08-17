using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps an existing Telepath view <see cref="Control"/> onto <see cref="ViewTarget"/>.
/// Injects; does not instantiate or free the node.
/// </summary>
public static class GodotViewTargets
{
    public static ViewTarget View(this Control view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new ViewTarget(
            present: viewModel => Present(view, viewModel),
            detach: () => Present(view, null));
    }

    private static void Present(Control view, IViewModel? viewModel)
    {
        if (viewModel is null)
        {
            ViewInjection.Clear(view);
            view.Visible = false;
            return;
        }

        view.Visible = true;
        ViewInjection.Inject(view, viewModel);
    }
}
