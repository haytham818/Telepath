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
