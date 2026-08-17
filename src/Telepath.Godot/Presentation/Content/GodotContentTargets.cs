using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps a slot <see cref="Control"/> onto <see cref="ContentTarget"/>.
/// </summary>
public static class GodotContentTargets
{
    public static ContentTarget Content(this Control slot, ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        var presenter = new ContentPresenter(slot, registry);
        return new ContentTarget(presenter.Present, presenter.Clear);
    }
}
