using System.Collections.Specialized;
using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Incremental overlay-stack binding onto <see cref="OverlayTarget"/>.
/// </summary>
public static class OverlayBindingExtensions
{
    public static void BindOverlay(
        this BindingSet bindings,
        ObservableList<IViewModel> source,
        OverlayTarget target)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target.Reset);
        ArgumentNullException.ThrowIfNull(target.Insert);
        ArgumentNullException.ThrowIfNull(target.RemoveAt);

        target.Reset(source);
        NotifyCollectionChangedEventHandler<IViewModel> handler = (in NotifyCollectionChangedEventArgs<IViewModel> e) =>
            Apply(in e, source, target);
        source.CollectionChanged += handler;
        bindings.Add(Disposable.Create(() => source.CollectionChanged -= handler));
        if (target.Detach is not null)
        {
            bindings.Add(Disposable.Create(target.Detach));
        }
    }

    private static void Apply(
        in NotifyCollectionChangedEventArgs<IViewModel> e,
        IReadOnlyList<IViewModel> source,
        OverlayTarget target)
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
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                target.Reset(source);
                return;
        }
    }
}
