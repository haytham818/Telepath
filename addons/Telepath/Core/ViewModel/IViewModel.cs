namespace Telepath;

/// <summary>
/// Platform-agnostic ViewModel contract. Lifetime is dispose-only;
/// enter/exit tree binding attach/detach belongs to the View layer.
/// </summary>
public interface IViewModel : IDisposable
{
    bool IsDisposed { get; }
}
