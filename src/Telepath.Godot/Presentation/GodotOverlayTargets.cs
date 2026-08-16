using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps overlay slots onto <see cref="OverlayTarget"/> / <see cref="IOverlayHost"/>.
/// </summary>
public static class GodotOverlayTargets
{
    public static OverlayTarget Overlays(this Control slot, ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        return new OverlayPresenter(slot, registry).Target;
    }

    public static void BindOverlayHost(
        this BindingSet bindings,
        IOverlayHost host,
        Control root,
        ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registry);
        new OverlayHostPresenter(root, registry).Bind(host, bindings);
    }
}
