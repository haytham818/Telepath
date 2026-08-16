using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps a slot <see cref="Control"/> onto <see cref="OverlayTarget"/>.
/// </summary>
public static class GodotOverlayTargets
{
    public static OverlayTarget Overlays(this Control slot, ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        return new OverlayPresenter(slot, registry).Target;
    }
}
