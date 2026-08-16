using System.Threading;
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
///         IncrementCommand = Command(() =&gt; Count.Value++);
///         SaveCommand = AsyncCommand(async ct =&gt; await SaveAsync(ct));
///     }
/// }
/// </code>
/// </example>
public abstract partial class ViewModel : IViewModel
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
    /// Called once before tracked disposables are released. Override to clean up
    /// non-<see cref="Track{T}"/> resources.
    /// </summary>
    protected virtual void OnDispose()
    {
    }
}
