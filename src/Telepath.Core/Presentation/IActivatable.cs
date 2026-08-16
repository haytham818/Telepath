namespace Telepath.Core;

/// <summary>
/// Optional presentation-lifetime hooks. The conductor calls these when a page
/// enters or leaves the foreground. Not part of <see cref="IViewModel"/>:
/// resource lifetime (bind / unbind / dispose) stays on the view.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Called after the page becomes <see cref="IConductor.ActiveItem"/>.
    /// The matching view, if any, is already bound.
    /// </summary>
    void Activate();

    /// <summary>
    /// Called before the page leaves the foreground. Does not unbind or dispose.
    /// </summary>
    void Deactivate();
}
