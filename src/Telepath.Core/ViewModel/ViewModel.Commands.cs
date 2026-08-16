using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Telepath.Core;

public abstract partial class ViewModel
{
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
