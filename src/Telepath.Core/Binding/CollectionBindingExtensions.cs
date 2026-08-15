using System.Collections.Specialized;
using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Collection bindings onto <see cref="CollectionTarget{T}"/>.
/// <see cref="ObservableList{T}"/> applies incremental mutations;
/// <see cref="Observable{T}"/> of a list replaces the whole target.
/// </summary>
public static class CollectionBindingExtensions
{
    public static void BindItems<T>(
        this BindingSet bindings,
        ObservableList<T> source,
        CollectionTarget<T> target)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        EnsureTarget(target);

        target.Reset(source);
        NotifyCollectionChangedEventHandler<T> handler = (in NotifyCollectionChangedEventArgs<T> e) =>
            Apply(in e, source, target);
        source.CollectionChanged += handler;
        bindings.Add(Disposable.Create(() => source.CollectionChanged -= handler));
        AttachDetach(bindings, target);
    }

    public static void BindItems<TSource, TTarget>(
        this BindingSet bindings,
        ObservableList<TSource> source,
        CollectionTarget<TTarget> target,
        Func<TSource, TTarget> convert)
    {
        ArgumentNullException.ThrowIfNull(convert);
        bindings.BindItems(source, ConvertTarget(target, convert));
    }

    public static void BindItems<TSource, TTarget>(
        this BindingSet bindings,
        ObservableList<TSource> source,
        CollectionTarget<TTarget> target,
        IValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        bindings.BindItems(source, ConvertTarget<TSource, TTarget>(target, converter.Convert));
    }

    public static void BindItems<TItem, TList>(
        this BindingSet bindings,
        Observable<TList> source,
        CollectionTarget<TItem> target)
        where TList : class, IReadOnlyList<TItem>
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        EnsureTarget(target);
        bindings.Add(source.Subscribe(items =>
            target.Reset(items ?? (IReadOnlyList<TItem>)Array.Empty<TItem>())));
        AttachDetach(bindings, target);
    }

    public static void BindItems<TSource, TTarget>(
        this BindingSet bindings,
        Observable<IReadOnlyList<TSource>> source,
        CollectionTarget<TTarget> target,
        Func<TSource, TTarget> convert)
    {
        ArgumentNullException.ThrowIfNull(convert);
        bindings.BindItems(source, ConvertTarget(target, convert));
    }

    public static void BindItems<TSource, TTarget>(
        this BindingSet bindings,
        Observable<IReadOnlyList<TSource>> source,
        CollectionTarget<TTarget> target,
        IValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        bindings.BindItems(source, ConvertTarget<TSource, TTarget>(target, converter.Convert));
    }

    private static void Apply<T>(
        in NotifyCollectionChangedEventArgs<T> e,
        IReadOnlyList<T> source,
        CollectionTarget<T> target)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.IsSingleItem)
                {
                    target.Insert(e.NewStartingIndex, e.NewItem);
                    return;
                }

                for (var i = 0; i < e.NewItems.Length; i++)
                {
                    target.Insert(e.NewStartingIndex + i, e.NewItems[i]);
                }

                return;

            case NotifyCollectionChangedAction.Remove:
                if (e.IsSingleItem)
                {
                    target.RemoveAt(e.OldStartingIndex);
                    return;
                }

                for (var i = e.OldItems.Length - 1; i >= 0; i--)
                {
                    target.RemoveAt(e.OldStartingIndex + i);
                }

                return;

            case NotifyCollectionChangedAction.Replace:
                if (e.IsSingleItem)
                {
                    target.Replace(e.NewStartingIndex, e.OldItem, e.NewItem);
                    return;
                }

                for (var i = 0; i < e.NewItems.Length; i++)
                {
                    target.Replace(e.NewStartingIndex + i, e.OldItems[i], e.NewItems[i]);
                }

                return;

            case NotifyCollectionChangedAction.Move:
                if (e.IsSingleItem)
                {
                    target.Move(e.OldStartingIndex, e.NewStartingIndex);
                    return;
                }

                target.Reset(source);
                return;

            case NotifyCollectionChangedAction.Reset:
                target.Reset(source);
                return;
        }
    }

    private static CollectionTarget<TSource> ConvertTarget<TSource, TTarget>(
        CollectionTarget<TTarget> target,
        Func<TSource, TTarget> convert)
    {
        EnsureTarget(target);
        return new CollectionTarget<TSource>(
            reset: items =>
            {
                if (items is null || items.Count == 0)
                {
                    target.Reset(Array.Empty<TTarget>());
                    return;
                }

                var converted = new TTarget[items.Count];
                for (var i = 0; i < items.Count; i++)
                {
                    converted[i] = convert(items[i]);
                }

                target.Reset(converted);
            },
            insert: (index, item) => target.Insert(index, convert(item)),
            removeAt: target.RemoveAt,
            replace: (index, oldItem, newItem) =>
                target.Replace(index, convert(oldItem), convert(newItem)),
            move: target.Move,
            detach: target.Detach);
    }

    private static void AttachDetach<T>(BindingSet bindings, CollectionTarget<T> target)
    {
        if (target.Detach is null)
        {
            return;
        }

        bindings.Add(Disposable.Create(target.Detach));
    }

    private static void EnsureTarget<T>(CollectionTarget<T> target)
    {
        ArgumentNullException.ThrowIfNull(target.Reset);
        ArgumentNullException.ThrowIfNull(target.Insert);
        ArgumentNullException.ThrowIfNull(target.RemoveAt);
        ArgumentNullException.ThrowIfNull(target.Replace);
        ArgumentNullException.ThrowIfNull(target.Move);
    }
}
