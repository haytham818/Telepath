using System.Threading;
using R3;

namespace Telepath.Core;

/// <summary>
/// Collects binding subscriptions for one attach cycle. Disposed when the view
/// leaves the tree; does not own the ViewModel.
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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _inner.Dispose();
    }
}
