using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Core.Tests;

public sealed class CollectionBindingTests
{
    [Fact]
    public void BindItemsCopiesObservableListAndAppliesMutations()
    {
        var source = new ObservableList<string> { "a" };
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target);

        Assert.Equal(new[] { "a" }, recording.Items);
        Assert.Equal(new[] { "reset" }, recording.Operations);

        source.Add("b");
        source.Insert(1, "x");
        source[0] = "A";
        source.Move(0, 2);
        source.RemoveAt(1);
        source.Clear();

        Assert.Equal(source, recording.Items);
        Assert.Equal(
            new[] { "reset", "insert:1", "insert:1", "replace:0", "move:0:2", "remove:1", "reset" },
            recording.Operations);
    }

    [Fact]
    public void BindItemsAppliesAddRange()
    {
        var source = new ObservableList<int> { 1 };
        var recording = new RecordingTarget<int>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target);
        source.AddRange(new[] { 2, 3 });

        Assert.Equal(new[] { 1, 2, 3 }, recording.Items);
        Assert.Equal(new[] { "reset", "insert:1", "insert:2" }, recording.Operations);
    }

    [Fact]
    public void BindItemsStopsAfterDispose()
    {
        var source = new ObservableList<string> { "a" };
        var recording = new RecordingTarget<string>();
        var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target);
        bindings.Dispose();
        source.Add("b");

        Assert.Equal(new[] { "a" }, recording.Items);
    }

    [Fact]
    public void BindItemsConvertsObservableList()
    {
        var source = new ObservableList<int> { 2 };
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target, static value => $"n:{value}");
        source.Add(5);

        Assert.Equal(new[] { "n:2", "n:5" }, recording.Items);
    }

    [Fact]
    public void BindItemsConvertsObservableListWithConverter()
    {
        var source = new ObservableList<int> { 3 };
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target, new PrefixConverter());

        Assert.Equal(new[] { "n:3" }, recording.Items);
    }

    [Fact]
    public void BindItemsReplacesSnapshotList()
    {
        using var source = new BindableReactiveProperty<IReadOnlyList<string>>(new[] { "a" });
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target);

        Assert.Equal(new[] { "a" }, recording.Items);
        source.Value = new[] { "b", "c" };
        Assert.Equal(new[] { "b", "c" }, recording.Items);
        Assert.All(recording.Operations, op => Assert.Equal("reset", op));
    }

    [Fact]
    public void BindItemsTreatsNullSnapshotAsEmpty()
    {
        var source = new Subject<IReadOnlyList<string>>();
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target);
        source.OnNext(new[] { "a" });
        source.OnNext(null!);

        Assert.Empty(recording.Items);
    }

    [Fact]
    public void BindItemsConvertsSnapshotList()
    {
        using var source = new BindableReactiveProperty<IReadOnlyList<int>>(new[] { 4 });
        var recording = new RecordingTarget<string>();
        using var bindings = new BindingSet();

        bindings.BindItems(source, recording.Target, static value => $"n:{value}");
        source.Value = new[] { 8, 9 };

        Assert.Equal(new[] { "n:8", "n:9" }, recording.Items);
    }

    [Fact]
    public void BindItemsInvokesDetachOnDispose()
    {
        var source = new ObservableList<string> { "a" };
        var recording = new RecordingTarget<string>();
        var detached = 0;
        var target = new CollectionTarget<string>(
            recording.Target.Reset,
            recording.Target.Insert,
            recording.Target.RemoveAt,
            recording.Target.Replace,
            recording.Target.Move,
            () => detached++);
        var bindings = new BindingSet();

        bindings.BindItems(source, target);
        bindings.Dispose();
        source.Add("b");

        Assert.Equal(1, detached);
        Assert.Equal(new[] { "a" }, recording.Items);
    }

    private sealed class RecordingTarget<T>
    {
        public List<T> Items { get; } = new();

        public List<string> Operations { get; } = new();

        public CollectionTarget<T> Target => new(
            reset: items =>
            {
                Items.Clear();
                Items.AddRange(items);
                Operations.Add("reset");
            },
            insert: (index, item) =>
            {
                Items.Insert(index, item);
                Operations.Add($"insert:{index}");
            },
            removeAt: index =>
            {
                Items.RemoveAt(index);
                Operations.Add($"remove:{index}");
            },
            replace: (index, _, item) =>
            {
                Items[index] = item;
                Operations.Add($"replace:{index}");
            },
            move: (oldIndex, newIndex) =>
            {
                var item = Items[oldIndex];
                Items.RemoveAt(oldIndex);
                Items.Insert(newIndex, item);
                Operations.Add($"move:{oldIndex}:{newIndex}");
            });
    }

    private sealed class PrefixConverter : IValueConverter<int, string>
    {
        public string Convert(int value) => $"n:{value}";
    }
}
