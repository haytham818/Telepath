using System.Reflection;
using Godot;
using Telepath.Core;

namespace Telepath.Godot;

internal static class ViewInjection
{
    public static void Inject(Control view, IViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        var property = GetViewModelProperty(view);
        if (!property.PropertyType.IsAssignableFrom(viewModel.GetType()))
        {
            throw new InvalidOperationException(
                $"Cannot inject '{viewModel.GetType().Name}' into '{view.GetType().Name}.ViewModel' ({property.PropertyType.Name}).");
        }

        property.SetValue(view, viewModel);
    }

    public static void Clear(Control view)
    {
        GetViewModelProperty(view).SetValue(view, null);
    }

    private static PropertyInfo GetViewModelProperty(Control view)
    {
        var property = view.GetType().GetProperty(
            "ViewModel",
            BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException(
                $"View '{view.GetType().Name}' does not implement ITelepathView<>.");
        }

        return property;
    }

    public static void Remove(Control view)
    {
        var parent = view.GetParent();
        parent?.RemoveChild(view);
        view.QueueFree();
    }
}
