namespace Telepath.Core;

/// <summary>
/// Host-agnostic collection view: incremental mutations plus full replace.
/// </summary>
public readonly struct CollectionTarget<T>
{
    public CollectionTarget(
        Action<IReadOnlyList<T>> reset,
        Action<int, T> insert,
        Action<int> removeAt,
        Action<int, T, T> replace,
        Action<int, int> move,
        Action? detach = null)
    {
        ArgumentNullException.ThrowIfNull(reset);
        ArgumentNullException.ThrowIfNull(insert);
        ArgumentNullException.ThrowIfNull(removeAt);
        ArgumentNullException.ThrowIfNull(replace);
        ArgumentNullException.ThrowIfNull(move);
        Reset = reset;
        Insert = insert;
        RemoveAt = removeAt;
        Replace = replace;
        Move = move;
        Detach = detach;
    }

    public Action<IReadOnlyList<T>> Reset { get; }

    public Action<int, T> Insert { get; }

    public Action<int> RemoveAt { get; }

    /// <summary>
    /// Replaces the item at <c>index</c>. Arguments are index, previous item, next item.
    /// </summary>
    public Action<int, T, T> Replace { get; }

    public Action<int, int> Move { get; }

    /// <summary>
    /// Optional cleanup invoked when the <see cref="BindingSet"/> is disposed.
    /// Native list widgets leave current items in place; container templates free generated children.
    /// </summary>
    public Action? Detach { get; }
}
