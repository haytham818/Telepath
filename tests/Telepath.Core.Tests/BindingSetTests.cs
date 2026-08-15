using R3;
using Telepath.Core;

namespace Telepath.Core.Tests;

public sealed class BindingSetTests
{
    [Fact]
    public void OneWayPushesValuesToTarget()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.OneWay(source, value => target = value);

        Assert.Equal("a", target);
        source.Value = "b";
        Assert.Equal("b", target);
    }

    [Fact]
    public void TwoWayWritesViewModelToTarget()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "unset";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.TwoWay(source, () => target, value => target = value, changed);

        Assert.Equal("a", target);
        source.Value = "b";
        Assert.Equal("b", target);
        Assert.Equal("b", source.Value);
    }

    [Fact]
    public void TwoWayWritesTargetChangesToViewModel()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "a";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.TwoWay(source, () => target, value => target = value, changed);

        target = "c";
        changed.OnNext("c");

        Assert.Equal("c", source.Value);
    }

    [Fact]
    public void TwoWayIgnoresReentryFromSetter()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "unset";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.TwoWay(
            source,
            () => target,
            value =>
            {
                target = value;
                changed.OnNext(value);
            },
            changed);

        source.Value = "b";

        Assert.Equal("b", target);
        Assert.Equal("b", source.Value);
    }

    [Fact]
    public void BindCommandExecutesOnTrigger()
    {
        var executed = 0;
        using var command = new ReactiveCommand(_ => executed++);
        var trigger = new Subject<Unit>();
        using var bindings = new BindingSet();

        bindings.BindCommand(command, trigger);
        trigger.OnNext(Unit.Default);

        Assert.Equal(1, executed);
    }

    [Fact]
    public void BindCommandSyncsCanExecute()
    {
        using var canExecute = new BindableReactiveProperty<bool>(true);
        using var command = canExecute.ToReactiveCommand(_ => { });
        var disabled = true;
        using var bindings = new BindingSet();

        bindings.BindCommand(command, new Subject<Unit>(), can => disabled = !can);

        Assert.False(disabled);
        canExecute.Value = false;
        Assert.True(disabled);
    }

    [Fact]
    public void BindCommandPassesParameterToExecute()
    {
        string? received = null;
        using var command = new ReactiveCommand<string>(value => received = value);
        var trigger = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.BindCommand(command, trigger);
        trigger.OnNext("query");

        Assert.Equal("query", received);
    }

    [Fact]
    public void DisposeIsIdempotentAndRejectsAdd()
    {
        var bindings = new BindingSet();
        bindings.Dispose();
        bindings.Dispose();

        Assert.True(bindings.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => bindings.Add(Disposable.Empty));
    }

    [Fact]
    public void OneWayConvertsWithFunc()
    {
        using var source = new BindableReactiveProperty<int>(2);
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.OneWay(source, value => target = value, static value => $"n:{value}");

        Assert.Equal("n:2", target);
        source.Value = 3;
        Assert.Equal("n:3", target);
    }

    [Fact]
    public void OneWayConvertsWithConverter()
    {
        using var source = new BindableReactiveProperty<int>(2);
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.OneWay(source, value => target = value, new PrefixConverter());

        Assert.Equal("n:2", target);
        source.Value = 4;
        Assert.Equal("n:4", target);
    }

    [Fact]
    public void TwoWayConvertsWithFunc()
    {
        using var source = new BindableReactiveProperty<int>(1);
        var target = "unset";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.TwoWay(
            source,
            () => target,
            value => target = value,
            changed,
            static value => $"n:{value}",
            static value => int.Parse(value.AsSpan(2)));

        Assert.Equal("n:1", target);
        source.Value = 5;
        Assert.Equal("n:5", target);

        target = "n:8";
        changed.OnNext("n:8");
        Assert.Equal(8, source.Value);
    }

    [Fact]
    public void TwoWayConvertsWithConverter()
    {
        using var source = new BindableReactiveProperty<int>(1);
        var target = "unset";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.TwoWay(
            source,
            () => target,
            value => target = value,
            changed,
            new PrefixConverter());

        Assert.Equal("n:1", target);
        source.Value = 6;
        Assert.Equal("n:6", target);

        target = "n:9";
        changed.OnNext("n:9");
        Assert.Equal(9, source.Value);
    }

    [Fact]
    public void BindOneWayWritesToTarget()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.Bind(source, BindingTarget<string>.OneWay(value => target = value));

        Assert.Equal("a", target);
        source.Value = "b";
        Assert.Equal("b", target);
    }

    [Fact]
    public void BindTwoWayWhenTargetSupportsIt()
    {
        using var source = new BindableReactiveProperty<string>("a");
        var target = "unset";
        var changed = new Subject<string>();
        using var bindings = new BindingSet();

        bindings.Bind(
            source,
            BindingTarget<string>.TwoWay(() => target, value => target = value, changed));

        Assert.Equal("a", target);
        source.Value = "b";
        Assert.Equal("b", target);

        target = "c";
        changed.OnNext("c");
        Assert.Equal("c", source.Value);
    }

    [Fact]
    public void BindBindablePropertyToOneWayTargetStaysOneWay()
    {
        using var source = new BindableReactiveProperty<int>(1);
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.Bind(source, BindingTarget<int>.OneWay(value => target = value.ToString()));

        Assert.Equal("1", target);
        source.Value = 2;
        Assert.Equal("2", target);
    }

    [Fact]
    public void BindConvertsWithFunc()
    {
        using var source = new BindableReactiveProperty<int>(2);
        var target = "unset";
        using var bindings = new BindingSet();

        bindings.Bind(
            source,
            BindingTarget<string>.OneWay(value => target = value),
            static value => $"n:{value}");

        Assert.Equal("n:2", target);
        source.Value = 3;
        Assert.Equal("n:3", target);
    }

    private sealed class PrefixConverter : ITwoWayValueConverter<int, string>
    {
        public string Convert(int value) => $"n:{value}";

        public int ConvertBack(string value) => int.Parse(value.AsSpan(2));
    }
}
