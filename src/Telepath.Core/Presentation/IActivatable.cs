namespace Telepath.Core;

/// <summary>
/// Optional presentation-lifetime hooks. A conductor or overlay calls these
/// when a page enters or leaves the foreground. Not part of <see cref="IViewModel"/>:
/// resource lifetime is driven by the view (<see cref="ViewModel.OnBound"/> /
/// <see cref="ViewModel.OnUnbound"/> / <see cref="ViewModel.OnDispose"/>).
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Called after the page becomes the foreground item.
    /// The matching view, if any, is already bound.
    /// </summary>
    void Activate();

    /// <summary>
    /// Called before the page leaves the foreground. Does not unbind or dispose.
    /// </summary>
    void Deactivate();
}
