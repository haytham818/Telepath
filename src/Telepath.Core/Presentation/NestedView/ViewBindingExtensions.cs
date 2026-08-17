using R3;

namespace Telepath.Core;

/// <summary>
/// One-way binding of a child ViewModel onto a <see cref="ViewTarget"/>.
/// </summary>
public static class ViewBindingExtensions
{
    public static void BindView(
        this BindingSet bindings,
        Observable<IViewModel?> source,
        ViewTarget target)
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

    public static void BindView<TViewModel>(
        this BindingSet bindings,
        Observable<TViewModel?> source,
        ViewTarget target)
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(source);
        bindings.BindView(source.Select(static viewModel => (IViewModel?)viewModel), target);
    }

    public static void BindView(
        this BindingSet bindings,
        IViewModel? viewModel,
        ViewTarget target)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(target.Present);

        target.Present(viewModel);
        if (target.Detach is not null)
        {
            bindings.Add(Disposable.Create(target.Detach));
        }
    }
}
