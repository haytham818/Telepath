namespace Telepath.Godot;

/// <summary>
/// Optional focus hand-off owned by a host view. Presenters call this when a
/// <see cref="Telepath.Core.OverlayLayer.BlocksPassThrough"/> overlay becomes
/// the input foreground. Views that do not implement it receive the first
/// focusable descendant.
/// </summary>
public interface IViewFocus
{
    /// <summary>
    /// Grabs GUI focus for this view. The node is in the tree and bound.
    /// Restoring a previously focused control after uncover is not this call.
    /// </summary>
    void TakeFocus();
}
