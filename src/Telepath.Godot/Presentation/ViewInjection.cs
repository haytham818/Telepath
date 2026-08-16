using System.Reflection;
using Godot;
using Telepath.Core;

namespace Telepath.Godot;

internal static class ViewInjection
{
    public static void Inject(Control view, IViewModel viewModel)
    {
        var property = view.GetType().GetProperty(
            "ViewModel",
            BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException(
                $"Instantiated view '{view.GetType().Name}' does not implement ITelepathView<>.");
        }

        if (!property.PropertyType.IsAssignableFrom(viewModel.GetType()))
        {
            throw new InvalidOperationException(
                $"Cannot inject '{viewModel.GetType().Name}' into '{view.GetType().Name}.ViewModel' ({property.PropertyType.Name}).");
        }

        property.SetValue(view, viewModel);
    }

    public static void Remove(Control view)
    {
        var parent = view.GetParent();
        parent?.RemoveChild(view);
        view.QueueFree();
    }
}
