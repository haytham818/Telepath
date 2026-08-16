namespace Telepath.Core;

/// <summary>
/// Host-agnostic nested view. <see cref="Present"/> injects a ViewModel into an
/// existing view; <see cref="Detach"/> clears the injection when the
/// <see cref="BindingSet"/> is disposed. Does not instantiate, free, or dispose.
/// </summary>
public readonly struct ViewTarget
{
    public ViewTarget(Action<IViewModel?> present, Action? detach = null)
    {
        ArgumentNullException.ThrowIfNull(present);
        Present = present;
        Detach = detach;
    }

    public Action<IViewModel?> Present { get; }

    /// <summary>
    /// Optional cleanup invoked when the <see cref="BindingSet"/> is disposed.
    /// Clears the nested view without freeing the node or disposing the ViewModel.
    /// </summary>
    public Action? Detach { get; }
}
