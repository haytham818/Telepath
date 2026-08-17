using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps overlay slots onto <see cref="OverlayTarget"/> / <see cref="IOverlayHost"/>.
/// </summary>
public static class GodotOverlayTargets
{
    public static OverlayTarget Overlays(
        this Control slot,
        ViewRegistry registry,
        PresentedViews? presented = null)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        return new OverlayPresenter(slot, registry, presented: presented).Target;
    }

    public static void BindOverlayHost(
        this BindingSet bindings,
        IOverlayHost host,
        Control root,
        ViewRegistry registry,
        PresentedViews? presented = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registry);
        new OverlayHostPresenter(root, registry, presented).Bind(host, bindings);
    }
}
