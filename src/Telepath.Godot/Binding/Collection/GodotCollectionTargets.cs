using System.Reflection;
using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps Godot list widgets onto <see cref="CollectionTarget{T}"/>.
/// </summary>
public static class GodotCollectionTargets
{
    public static CollectionTarget<string> Items(this ItemList list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return new CollectionTarget<string>(
            reset: items => Reset(list, items),
            insert: (index, item) =>
            {
                list.AddItem(item ?? string.Empty);
                var last = list.ItemCount - 1;
                if (index < last)
                {
                    list.MoveItem(last, index);
                }
            },
            removeAt: list.RemoveItem,
            replace: (index, _, item) => list.SetItemText(index, item ?? string.Empty),
            move: list.MoveItem);
    }

    public static CollectionTarget<string> Items(this OptionButton button)
    {
        ArgumentNullException.ThrowIfNull(button);
        return new CollectionTarget<string>(
            reset: items => Reset(button, items),
            insert: (index, item) =>
            {
                var items = Snapshot(button);
                items.Insert(index, item ?? string.Empty);
                Reset(button, items);
            },
            removeAt: button.RemoveItem,
            replace: (index, _, item) => button.SetItemText(index, item ?? string.Empty),
            move: (oldIndex, newIndex) =>
            {
                var items = Snapshot(button);
                var value = items[oldIndex];
                items.RemoveAt(oldIndex);
                items.Insert(newIndex, value);
                Reset(button, items);
            });
    }

    private static void Reset(ItemList list, IReadOnlyList<string> items)
    {
        var selected = list.GetSelectedItems();
        var selectedIndex = selected.Length == 0 ? -1 : selected[0];
        list.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            list.AddItem(items[i] ?? string.Empty);
        }

        if ((uint)selectedIndex < (uint)list.ItemCount)
        {
            list.Select(selectedIndex);
        }
    }

    private static void Reset(OptionButton button, IReadOnlyList<string> items)
    {
        var selected = button.Selected;
        button.Clear();
        for (var i = 0; i < items.Count; i++)
        {
            button.AddItem(items[i] ?? string.Empty);
        }

        if ((uint)selected < (uint)button.ItemCount)
        {
            button.Select(selected);
        }
    }

    private static List<string> Snapshot(OptionButton button)
    {
        var items = new List<string>(button.ItemCount);
        for (var i = 0; i < button.ItemCount; i++)
        {
            items.Add(button.GetItemText(i));
        }

        return items;
    }

    public static CollectionTarget<T> Items<T>(this Container container, Func<T, Control> create)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(create);
        var created = new List<Control>();
        return new CollectionTarget<T>(
            reset: items =>
            {
                RemoveAll(created);
                for (var i = 0; i < items.Count; i++)
                {
                    Add(container, created, create(items[i]), i);
                }
            },
            insert: (index, item) => Add(container, created, create(item), index),
            removeAt: index => RemoveAt(created, index),
            replace: (index, _, item) =>
            {
                RemoveAt(created, index);
                Add(container, created, create(item), index);
            },
            move: (oldIndex, newIndex) =>
            {
                var view = created[oldIndex];
                created.RemoveAt(oldIndex);
                created.Insert(newIndex, view);
                container.MoveChild(view, newIndex);
            },
            detach: () => RemoveAll(created));
    }

    public static CollectionTarget<TViewModel> Items<TView, TViewModel>(
        this Container container,
        PackedScene scene)
        where TView : Control, ITelepathView<TViewModel>
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(scene);
        return container.Items<TViewModel>(item =>
        {
            var view = scene.Instantiate<TView>();
            view.ViewModel = item;
            return view;
        });
    }

    public static CollectionTarget<TViewModel> Items<TViewModel>(
        this Container container,
        PackedScene scene)
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(scene);
        return container.Items<TViewModel>(item =>
        {
            var view = scene.Instantiate<Control>();
            if (view is ITelepathView<TViewModel> typed)
            {
                typed.ViewModel = item;
                return view;
            }

            var property = view.GetType().GetProperty("ViewModel");
            if (property is not null && property.CanWrite)
            {
                property.SetValue(view, item);
                return view;
            }

            throw new InvalidOperationException(
                $"Instantiated item view '{view.GetType().Name}' does not implement ITelepathView<{typeof(TViewModel).Name}>.");
        });
    }

    private static void Add(Container container, List<Control> created, Control view, int index)
    {
        created.Insert(index, view);
        container.AddChild(view);
        if (index < container.GetChildCount() - 1)
        {
            container.MoveChild(view, index);
        }
    }

    private static void RemoveAt(List<Control> created, int index)
    {
        var view = created[index];
        created.RemoveAt(index);
        Remove(view);
    }

    private static void RemoveAll(List<Control> created)
    {
        for (var i = created.Count - 1; i >= 0; i--)
        {
            Remove(created[i]);
        }

        created.Clear();
    }

    private static void Remove(Control view)
    {
        var parent = view.GetParent();
        parent?.RemoveChild(view);
        view.QueueFree();
    }
}
