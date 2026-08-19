using System.Reflection;
using QFramework;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed class QfViewModelFactory : IViewModelActivator, IUtility, ICanGetUtility
{
    public IArchitecture GetArchitecture() => ShowcaseApp.Interface;

    public T Create<T>(params object[] arguments) where T : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var type = typeof(T);
        var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"No public constructor on '{type.Name}'.");
        }

        foreach (var constructor in constructors.OrderByDescending(static ctor => ctor.GetParameters().Length))
        {
            if (TryCreate(constructor, arguments, out var instance) && instance is T viewModel)
            {
                return viewModel;
            }
        }

        throw new InvalidOperationException(
            $"Cannot resolve '{type.Name}' from QFramework utilities.");
    }

    private bool TryCreate(ConstructorInfo constructor, object[] arguments, out object? instance)
    {
        instance = null;
        var parameters = constructor.GetParameters();
        var values = new object?[parameters.Length];
        var used = new bool[arguments.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (TryTakeExtra(parameter.ParameterType, arguments, used, out var extra))
            {
                values[i] = extra;
                continue;
            }

            if (TryResolve(parameter.ParameterType, out var resolved))
            {
                values[i] = resolved;
                continue;
            }

            if (parameter.HasDefaultValue)
            {
                values[i] = parameter.DefaultValue;
                continue;
            }

            return false;
        }

        instance = constructor.Invoke(values);
        return true;
    }

    private bool TryResolve(Type type, out object? value)
    {
        var services = this.GetUtility<ShellServices>()
            ?? throw new InvalidOperationException("ShowcaseApp.BindShell has not run.");

        if (type == typeof(IViewModelActivator) || type == typeof(QfViewModelFactory))
        {
            value = this;
            return true;
        }

        if (type == typeof(INavigator))
        {
            value = services.Navigator;
            return true;
        }

        if (type == typeof(IOverlayHost))
        {
            value = services.Overlay;
            return true;
        }

        if (type == typeof(IOverlay))
        {
            value = services.Overlay;
            return true;
        }

        if (type == typeof(IInteraction))
        {
            value = services.Interaction;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryTakeExtra(Type type, object[] arguments, bool[] used, out object? extra)
    {
        for (var i = 0; i < arguments.Length; i++)
        {
            if (used[i] || arguments[i] is null || !type.IsInstanceOfType(arguments[i]))
            {
                continue;
            }

            used[i] = true;
            extra = arguments[i];
            return true;
        }

        extra = null;
        return false;
    }
}
