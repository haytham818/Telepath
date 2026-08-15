using System.Threading;
using Godot;
using R3;

namespace Telepath.Godot;

/// <summary>
/// Collects binding subscriptions for one attach cycle. Disposed on exit tree;
/// does not own the ViewModel.
/// </summary>
public sealed class BindingSet : IDisposable
{
    private readonly CompositeDisposable _inner = new();
    private int _disposed;

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void Add(IDisposable disposable)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _inner.Add(disposable);
    }

    public void BindLabel(Observable<string> source, Label label)
    {
        Add(source.SubscribeToLabel(label));
    }

    /// <summary>
    /// Wires <paramref name="button"/> pressed to <paramref name="command"/> and
    /// mirrors <c>CanExecute</c> onto <see cref="BaseButton.Disabled"/>.
    /// </summary>
    public void BindCommand(ReactiveCommand command, BaseButton button)
    {
        Add(button.OnPressedAsObservable().Subscribe(_ => command.Execute(Unit.Default)));

        void SyncDisabled() => button.Disabled = !command.CanExecute();
        SyncDisabled();

        EventHandler handler = (_, _) => SyncDisabled();
        command.CanExecuteChanged += handler;
        Add(Disposable.Create(() => command.CanExecuteChanged -= handler));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _inner.Dispose();
    }
}
