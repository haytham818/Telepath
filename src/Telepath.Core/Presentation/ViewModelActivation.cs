namespace Telepath.Core;

internal static class ViewModelActivation
{
    public static T Create<T>(IViewModelActivator? activator, object[] arguments)
        where T : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (activator is null)
        {
            throw new InvalidOperationException(
                "Set ViewModelActivator before navigating or pushing a ViewModel type.");
        }

        return activator.Create<T>(arguments);
    }
}
