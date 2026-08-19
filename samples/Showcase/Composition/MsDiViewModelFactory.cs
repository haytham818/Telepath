using Microsoft.Extensions.DependencyInjection;
using Telepath.Core;

namespace Telepath.Showcase;

/// <summary>
/// Fills constructor parameters from the container. The returned instance is not
/// tracked by the provider, so Conductor / OverlayHost remain the owners.
/// </summary>
public sealed class MsDiViewModelFactory : IViewModelActivator
{
    private readonly IServiceProvider _services;

    public MsDiViewModelFactory(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _services = services;
    }

    public T Create<T>(params object[] arguments) where T : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Length == 0
            ? ActivatorUtilities.CreateInstance<T>(_services)
            : (T)ActivatorUtilities.CreateInstance(_services, typeof(T), arguments);
    }
}
