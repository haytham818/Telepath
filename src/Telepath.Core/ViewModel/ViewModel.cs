using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Telepath.Core;

/// <summary>
/// Base class for Telepath ViewModels. Tracks R3 disposables via <see cref="Track{T}"/>
/// and releases them on <see cref="Dispose"/>. Does not implement INPC; notifications
/// live on <c>BindableReactiveProperty&lt;T&gt;</c> / commands.
/// </summary>
/// <example>
/// <code>
/// public sealed class CounterViewModel : ViewModel
/// {
///     public BindableReactiveProperty&lt;int&gt; Count { get; }
///     public ReactiveCommand IncrementCommand { get; }
///     public ReactiveCommand SaveCommand { get; }
///
///     public CounterViewModel()
///     {
///         Count = Track(new BindableReactiveProperty&lt;int&gt;(0));
///         IncrementCommand = Track(new ReactiveCommand(_ =&gt; Count.Value++));
///         SaveCommand = AsyncCommand(async ct =&gt; await SaveAsync(ct));
///     }
/// }
/// </code>
/// </example>
public abstract class ViewModel : IViewModel
{
    // Keep DisposableBag private: it is a mutable struct and must not be copied.
    private DisposableBag _disposables;
    private int _disposed; // 0 = alive, 1 = disposed

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    /// Registers <paramref name="disposable"/> for release on <see cref="Dispose"/>.
    /// Throws if this ViewModel is already disposed.
    /// </summary>
    protected T Track<T>(T disposable)
        where T : IDisposable
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return disposable.AddTo(ref _disposables);
    }

    /// <summary>
    /// Idempotent dispose. Calls <see cref="OnDispose"/> then releases tracked disposables.
    /// Do not override; override <see cref="OnDispose"/> instead.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            OnDispose();
        }
        finally
        {
            _disposables.Dispose();
        }
    }

    /// <summary>
    /// Creates a tracked async command. Default <see cref="AwaitOperation.Drop"/>
    /// ignores overlapping executes, <see cref="ReactiveCommand.CanExecute"/> is
    /// false while running, and <see cref="Dispose"/> cancels the token.
    /// </summary>
    protected ReactiveCommand AsyncCommand(
        Func<CancellationToken, ValueTask> execute,
        Observable<bool>? canExecute = null,
        AwaitOperation awaitOperation = AwaitOperation.Drop)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var executing = Track(new ReactiveProperty<bool>(false));
        var command = new ReactiveCommand(
            CreateCanExecute(canExecute, executing, awaitOperation),
            initialCanExecute: true);
        Track(command.SubscribeAwait(
            async (_, cancellationToken) =>
                await RunAsync(executing, cancellationToken, execute).ConfigureAwait(true),
            awaitOperation,
            configureAwait: true,
            cancelOnCompleted: true));
        return Track(command);
    }

    /// <summary>
    /// Creates a tracked parameterized async command. Default
    /// <see cref="AwaitOperation.Drop"/> ignores overlapping executes,
    /// <see cref="ReactiveCommand{T}.CanExecute"/> is false while running, and
    /// <see cref="Dispose"/> cancels the token.
    /// </summary>
    protected ReactiveCommand<T> AsyncCommand<T>(
        Func<T, CancellationToken, ValueTask> execute,
        Observable<bool>? canExecute = null,
        AwaitOperation awaitOperation = AwaitOperation.Drop)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var executing = Track(new ReactiveProperty<bool>(false));
        return Track(CreateCanExecute(canExecute, executing, awaitOperation)
            .ToReactiveCommand<T>(
                async (argument, cancellationToken) =>
                    await RunAsync(
                            executing,
                            cancellationToken,
                            token => execute(argument, token))
                        .ConfigureAwait(true),
                initialCanExecute: true,
                awaitOperation,
                configureAwait: true,
                cancelOnCompleted: true));
    }

    /// <summary>
    /// Called once before tracked disposables are released. Override to clean up
    /// non-<see cref="Track{T}"/> resources.
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    private async ValueTask RunAsync(
        ReactiveProperty<bool> executing,
        CancellationToken cancellationToken,
        Func<CancellationToken, ValueTask> execute)
    {
        executing.Value = true;
        try
        {
            await execute(cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!IsDisposed && !executing.IsDisposed)
            {
                executing.Value = false;
            }
        }
    }

    private static Observable<bool> CreateCanExecute(
        Observable<bool>? canExecute,
        ReactiveProperty<bool> executing,
        AwaitOperation awaitOperation)
    {
        var disableWhenExecuting = awaitOperation
            is AwaitOperation.Drop
            or AwaitOperation.Sequential;
        if (!disableWhenExecuting)
        {
            return canExecute ?? Observable.Return(true);
        }

        return canExecute is null
            ? executing.Select(static busy => !busy)
            : Observable.CombineLatest(
                canExecute,
                executing,
                static (can, busy) => can && !busy);
    }
}
