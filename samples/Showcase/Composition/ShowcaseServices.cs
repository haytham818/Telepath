using Microsoft.Extensions.DependencyInjection;
using Telepath.Core;

namespace Telepath.Showcase;

public static class ShowcaseServices
{
    public static IServiceCollection AddShowcase(
        this IServiceCollection services,
        INavigator navigator,
        IOverlayHost overlay,
        IInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(interaction);

        services.AddSingleton(navigator);
        services.AddSingleton(overlay);
        services.AddSingleton<IOverlay>(overlay);
        services.AddSingleton(interaction);
        services.AddSingleton<IViewModelActivator, MsDiViewModelFactory>();
        return services;
    }
}
