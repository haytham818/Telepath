using R3;

namespace Telepath.Core;

/// <summary>
/// Host-agnostic view property: set always, get/changed only when two-way capable.
/// </summary>
public readonly struct BindingTarget<T>
{
    /// <summary>
    /// One-way binding target.
    /// </summary>
    public BindingTarget(Action<T> set)
    {
        ArgumentNullException.ThrowIfNull(set);
        Set = set;
        Get = null;
        Changed = null;
    }

    /// <summary>
    /// Two-way binding target.
    /// </summary>
    public BindingTarget(Func<T> get, Action<T> set, Observable<T> changed)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(changed);
        Get = get;
        Set = set;
        Changed = changed;
    }

    public Action<T> Set { get; }

    public Func<T>? Get { get; }

    public Observable<T>? Changed { get; }

    public bool SupportsTwoWay => Get is not null && Changed is not null;

    public static BindingTarget<T> OneWay(Action<T> set) => new(set);

    public static BindingTarget<T> TwoWay(Func<T> get, Action<T> set, Observable<T> changed)
        => new(get, set, changed);
}
