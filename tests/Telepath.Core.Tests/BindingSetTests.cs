using R3;

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
}
