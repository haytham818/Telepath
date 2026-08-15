using System.Threading;
using R3;

namespace Telepath.Core;

/// <summary>
/// Platform-agnostic binding primitives. Host adapters map control properties onto these.
/// </summary>
public static class BindingExtensions
{
    public static void OneWay<T>(this BindingSet bindings, Observable<T> source, Action<T> set)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(set);
        bindings.Add(source.Subscribe(set));
    }

    public static void TwoWay<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        Func<T> get,
        Action<T> set,
        Observable<T> changed)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(changed);

        var gate = 0;
        bindings.Add(source.Subscribe(value =>
        {
            if (Interlocked.Exchange(ref gate, 1) != 0)
            {
                return;
            }

            try
            {
                if (!EqualityComparer<T>.Default.Equals(get(), value))
                {
                    set(value);
                }
            }
            finally
            {
                Volatile.Write(ref gate, 0);
            }
        }));

        bindings.Add(changed.Subscribe(value =>
        {
            if (Interlocked.Exchange(ref gate, 1) != 0)
            {
                return;
            }

            try
            {
                if (!EqualityComparer<T>.Default.Equals(source.Value, value))
                {
                    source.Value = value;
                }
            }
            finally
            {
                Volatile.Write(ref gate, 0);
            }
        }));
    }

    public static void OneWay<TSource, TTarget>(
        this BindingSet bindings,
        Observable<TSource> source,
        Action<TTarget> set,
        Func<TSource, TTarget> convert)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(convert);
        bindings.OneWay(source, value => set(convert(value)));
    }

    public static void OneWay<TSource, TTarget>(
        this BindingSet bindings,
        Observable<TSource> source,
        Action<TTarget> set,
        IValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        bindings.OneWay(source, set, converter.Convert);
    }

    public static void TwoWay<TSource, TTarget>(
        this BindingSet bindings,
        BindableReactiveProperty<TSource> source,
        Func<TTarget> get,
        Action<TTarget> set,
        Observable<TTarget> changed,
        Func<TSource, TTarget> convert,
        Func<TTarget, TSource> convertBack)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(changed);
        ArgumentNullException.ThrowIfNull(convert);
        ArgumentNullException.ThrowIfNull(convertBack);

        var gate = 0;
        bindings.Add(source.Subscribe(value =>
        {
            if (Interlocked.Exchange(ref gate, 1) != 0)
            {
                return;
            }

            try
            {
                var converted = convert(value);
                if (!EqualityComparer<TTarget>.Default.Equals(get(), converted))
                {
                    set(converted);
                }
            }
            finally
            {
                Volatile.Write(ref gate, 0);
            }
        }));

        bindings.Add(changed.Subscribe(value =>
        {
            if (Interlocked.Exchange(ref gate, 1) != 0)
            {
                return;
            }

            try
            {
                var converted = convertBack(value);
                if (!EqualityComparer<TSource>.Default.Equals(source.Value, converted))
                {
                    source.Value = converted;
                }
            }
            finally
            {
                Volatile.Write(ref gate, 0);
            }
        }));
    }

    public static void TwoWay<TSource, TTarget>(
        this BindingSet bindings,
        BindableReactiveProperty<TSource> source,
        Func<TTarget> get,
        Action<TTarget> set,
        Observable<TTarget> changed,
        ITwoWayValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        bindings.TwoWay(source, get, set, changed, converter.Convert, converter.ConvertBack);
    }

    public static void Bind<T>(this BindingSet bindings, Observable<T> source, BindingTarget<T> target)
    {
        ArgumentNullException.ThrowIfNull(target.Set);
        bindings.OneWay(source, target.Set);
    }

    public static void Bind<T>(this BindingSet bindings, BindableReactiveProperty<T> source, BindingTarget<T> target)
    {
        ArgumentNullException.ThrowIfNull(target.Set);
        if (target.SupportsTwoWay)
        {
            bindings.TwoWay(source, target.Get!, target.Set, target.Changed!);
            return;
        }

        bindings.OneWay(source, target.Set);
    }

    public static void Bind<TSource, TTarget>(
        this BindingSet bindings,
        Observable<TSource> source,
        BindingTarget<TTarget> target,
        Func<TSource, TTarget> convert)
    {
        ArgumentNullException.ThrowIfNull(target.Set);
        bindings.OneWay(source, target.Set, convert);
    }

    public static void Bind<TSource, TTarget>(
        this BindingSet bindings,
        Observable<TSource> source,
        BindingTarget<TTarget> target,
        IValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(target.Set);
        bindings.OneWay(source, target.Set, converter);
    }

    public static void Bind<TSource, TTarget>(
        this BindingSet bindings,
        BindableReactiveProperty<TSource> source,
        BindingTarget<TTarget> target,
        Func<TSource, TTarget> convert,
        Func<TTarget, TSource> convertBack)
    {
        ArgumentNullException.ThrowIfNull(target.Set);
        if (target.SupportsTwoWay)
        {
            bindings.TwoWay(source, target.Get!, target.Set, target.Changed!, convert, convertBack);
            return;
        }

        bindings.OneWay(source, target.Set, convert);
    }

    public static void Bind<TSource, TTarget>(
        this BindingSet bindings,
        BindableReactiveProperty<TSource> source,
        BindingTarget<TTarget> target,
        ITwoWayValueConverter<TSource, TTarget> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        bindings.Bind(source, target, converter.Convert, converter.ConvertBack);
    }

    public static void BindCommand(
        this BindingSet bindings,
        ReactiveCommand command,
        Observable<Unit> execute,
        Action<bool>? setCanExecute = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(execute);

        bindings.Add(execute.Subscribe(_ => command.Execute(Unit.Default)));
        if (setCanExecute is null)
        {
            return;
        }

        void Sync() => setCanExecute(command.CanExecute());
        Sync();

        EventHandler handler = (_, _) => Sync();
        command.CanExecuteChanged += handler;
        bindings.Add(Disposable.Create(() => command.CanExecuteChanged -= handler));
    }

    public static void BindCommand<T>(
        this BindingSet bindings,
        ReactiveCommand<T> command,
        Observable<T> execute,
        Action<bool>? setCanExecute = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(execute);

        bindings.Add(execute.Subscribe(value => command.Execute(value)));
        if (setCanExecute is null)
        {
            return;
        }

        void Sync() => setCanExecute(command.CanExecute());
        Sync();

        EventHandler handler = (_, _) => Sync();
        command.CanExecuteChanged += handler;
        bindings.Add(Disposable.Create(() => command.CanExecuteChanged -= handler));
    }
}
