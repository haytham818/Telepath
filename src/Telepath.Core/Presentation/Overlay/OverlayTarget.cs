namespace Telepath.Core;

/// <summary>
/// Host-agnostic stacked overlay views. Mutations match
/// <see cref="ObservableList{T}"/>; <see cref="Detach"/> clears generated
/// children when the <see cref="BindingSet"/> is disposed. Does not own ViewModels.
/// </summary>
public readonly struct OverlayTarget
{
    public OverlayTarget(
        Action<IReadOnlyList<IViewModel>> reset,
        Action<int, IViewModel> insert,
        Action<int> removeAt,
        Action? detach = null)
    {
        ArgumentNullException.ThrowIfNull(reset);
        ArgumentNullException.ThrowIfNull(insert);
        ArgumentNullException.ThrowIfNull(removeAt);
        Reset = reset;
        Insert = insert;
        RemoveAt = removeAt;
        Detach = detach;
    }

    public Action<IReadOnlyList<IViewModel>> Reset { get; }

    public Action<int, IViewModel> Insert { get; }

    public Action<int> RemoveAt { get; }

    public Action? Detach { get; }
}
