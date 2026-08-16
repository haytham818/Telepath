#if TOOLS
using Telepath.Core;

namespace Telepath.Godot.Editor;

internal static class ConverterCatalog
{
    private static IReadOnlyList<Type>? _converters;
    private static IReadOnlyList<Type>? _itemViews;

    public static IReadOnlyList<Type> Scan()
        => _converters ??= ScanConverters();

    public static IReadOnlyList<Type> ScanItemViews()
        => _itemViews ??= ScanViews();

    private static IReadOnlyList<Type> ScanConverters()
    {
        var converters = new List<Type>();
        foreach (var type in EditorTypeScan.SafeTypes())
        {
            if (type.IsAbstract
                || type.IsGenericTypeDefinition
                || type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            if (type.GetInterfaces().Any(static iface =>
                    iface.IsGenericType
                    && iface.GetGenericTypeDefinition() == typeof(IValueConverter<,>)))
            {
                converters.Add(type);
            }
        }

        converters.Sort(static (left, right) =>
            string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
        return converters;
    }

    private static IReadOnlyList<Type> ScanViews()
    {
        var views = new List<Type>();
        foreach (var type in EditorTypeScan.SafeTypes())
        {
            if (ViewScriptResolver.TryGetViewModel(type, out _))
            {
                views.Add(type);
            }
        }

        views.Sort(static (left, right) =>
            string.Compare(left.FullName, right.FullName, StringComparison.Ordinal));
        return views;
    }
}
#endif
