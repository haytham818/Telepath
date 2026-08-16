using R3;

namespace Telepath.Core.Tests;

public sealed class ViewModelCommandTests
{
    [Fact]
    public async Task AsyncCommandDisablesWhileExecuting()
    {
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        using var viewModel = new TestViewModel();
        var command = viewModel.Create(async cancellationToken =>
        {
            started.SetResult();
            await release.Task.WaitAsync(cancellationToken);
        });

        Assert.True(command.CanExecute());
        command.Execute(Unit.Default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(command.CanExecute());
        release.SetResult();
        await WaitUntilAsync(() => command.CanExecute());
    }

    [Fact]
    public async Task AsyncCommandDropsOverlappingExecute()
    {
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var runs = 0;
        using var viewModel = new TestViewModel();
        var command = viewModel.Create(async cancellationToken =>
        {
            runs++;
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
        });

        command.Execute(Unit.Default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        command.Execute(Unit.Default);
        release.SetResult();
        await WaitUntilAsync(() => command.CanExecute());

        Assert.Equal(1, runs);
    }

    [Fact]
    public void AsyncCommandMirrorsCanExecute()
    {
        using var canExecute = new BindableReactiveProperty<bool>(false);
        using var viewModel = new TestViewModel();
        var command = viewModel.Create(_ => ValueTask.CompletedTask, canExecute);

        Assert.False(command.CanExecute());
        canExecute.Value = true;
        Assert.True(command.CanExecute());
        canExecute.Value = false;
        Assert.False(command.CanExecute());
    }

    [Fact]
    public async Task AsyncCommandPassesParameter()
    {
        string? received = null;
        using var viewModel = new TestViewModel();
        var command = viewModel.Create<string>(async (value, _) =>
        {
            received = value;
            await Task.CompletedTask;
        });

        command.Execute("query");
        await WaitUntilAsync(() => received == "query");
    }

    [Fact]
    public async Task DisposeCancelsInFlightAsyncCommand()
    {
        var started = new TaskCompletionSource();
        var cancelled = new TaskCompletionSource();
        var viewModel = new TestViewModel();
        var command = viewModel.Create(async cancellationToken =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult();
                throw;
            }
        });

        command.Execute(Unit.Default);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        viewModel.Dispose();
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CommandExecutesAndPassesParameter()
    {
        using var viewModel = new TestViewModel();
        var runs = 0;
        string? received = null;
        var increment = viewModel.Create(() => runs++);
        var search = viewModel.Create<string>(value => received = value);

        increment.Execute(Unit.Default);
        search.Execute("query");

        Assert.Equal(1, runs);
        Assert.Equal("query", received);
    }

    [Fact]
    public void CommandMirrorsCanExecute()
    {
        using var canExecute = new BindableReactiveProperty<bool>(false);
        using var viewModel = new TestViewModel();
        var command = viewModel.Create(() => { }, canExecute);

        Assert.False(command.CanExecute());
        canExecute.Value = true;
        Assert.True(command.CanExecute());
        canExecute.Value = false;
        Assert.False(command.CanExecute());
    }

    [Fact]
    public void CommandThrowsWhenViewModelAlreadyDisposed()
    {
        var viewModel = new TestViewModel();
        viewModel.Dispose();

        Assert.Throws<ObjectDisposedException>(() => viewModel.Create(() => { }));
    }

    [Fact]
    public void AsyncCommandThrowsWhenViewModelAlreadyDisposed()
    {
        var viewModel = new TestViewModel();
        viewModel.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            viewModel.Create(_ => ValueTask.CompletedTask));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Condition was not met.");
            }

            await Task.Yield();
        }
    }

    private sealed class TestViewModel : ViewModel
    {
        public ReactiveCommand Create(Action execute, Observable<bool>? canExecute = null)
            => Command(execute, canExecute);

        public ReactiveCommand<T> Create<T>(Action<T> execute, Observable<bool>? canExecute = null)
            => Command(execute, canExecute);

        public ReactiveCommand Create(
            Func<CancellationToken, ValueTask> execute,
            Observable<bool>? canExecute = null)
            => AsyncCommand(execute, canExecute);

        public ReactiveCommand<T> Create<T>(
            Func<T, CancellationToken, ValueTask> execute,
            Observable<bool>? canExecute = null)
            => AsyncCommand(execute, canExecute);
    }
}
