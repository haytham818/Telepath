namespace Telepath.Core;

/// <summary>
/// Platform-agnostic ViewModel contract. The interface itself is dispose-only;
/// enter/exit tree binding is driven by the View, which then notifies
/// <see cref="ViewModel.OnBound"/> / <see cref="ViewModel.OnUnbound"/>.
/// </summary>
public interface IViewModel : IDisposable
{
    bool IsDisposed { get; }
}
