namespace Telepath.Core;

/// <summary>
/// Host-agnostic single-slot content view. <see cref="Present"/> replaces the
/// shown page; <see cref="Detach"/> clears it when the <see cref="BindingSet"/>
/// is disposed. Does not own or dispose the ViewModel.
/// </summary>
public readonly struct ContentTarget
{
    public ContentTarget(Action<IViewModel?> present, Action? detach = null)
    {
        ArgumentNullException.ThrowIfNull(present);
        Present = present;
        Detach = detach;
    }

    public Action<IViewModel?> Present { get; }

    /// <summary>
    /// Optional cleanup invoked when the <see cref="BindingSet"/> is disposed.
    /// Frees generated child views without disposing their ViewModels.
    /// </summary>
    public Action? Detach { get; }
}
