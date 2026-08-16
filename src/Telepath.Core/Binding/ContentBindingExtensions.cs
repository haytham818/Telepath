using R3;

namespace Telepath.Core;

/// <summary>
/// One-way binding of the active page onto a <see cref="ContentTarget"/>.
/// </summary>
public static class ContentBindingExtensions
{
    public static void BindContent(
        this BindingSet bindings,
        Observable<IViewModel?> source,
        ContentTarget target)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target.Present);

        bindings.Add(source.Subscribe(target.Present));
        if (target.Detach is not null)
        {
            bindings.Add(Disposable.Create(target.Detach));
        }
    }
}
