#if TOOLS
#nullable enable
using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Godot.Editor;

internal enum ViewModelMemberKind
{
    Property,
    Command,
    Items,
}

internal sealed record ViewModelMember(string Name, ViewModelMemberKind Kind, Type ValueType)
{
    public string Display => Kind switch
    {
        ViewModelMemberKind.Command => ValueType == typeof(Unit)
            ? $"{Name}  ·  command"
            : $"{Name}  ·  command<{ValueType.Name}>",
        ViewModelMemberKind.Items => $"{Name}  ·  list<{ValueType.Name}>",
        _ => $"{Name}  ·  {ValueType.Name}",
    };
}

internal static class ViewModelMemberScanner
{
    public static IReadOnlyList<ViewModelMember> Scan(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        var members = new List<ViewModelMember>();
        foreach (var property in viewModelType.GetProperties(
                     System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            var type = property.PropertyType;
            if (TryGeneric(type, typeof(BindableReactiveProperty<>), out var valueType))
            {
                members.Add(new ViewModelMember(property.Name, ViewModelMemberKind.Property, valueType));
                continue;
            }

            if (type == typeof(ReactiveCommand))
            {
                members.Add(new ViewModelMember(property.Name, ViewModelMemberKind.Command, typeof(Unit)));
                continue;
            }

            if (TryGeneric(type, typeof(ReactiveCommand<>), out var argumentType))
            {
                members.Add(new ViewModelMember(property.Name, ViewModelMemberKind.Command, argumentType));
                continue;
            }

            if (TryGeneric(type, typeof(ObservableList<>), out var itemType))
            {
                members.Add(new ViewModelMember(property.Name, ViewModelMemberKind.Items, itemType));
                continue;
            }

            if (typeof(IViewModel).IsAssignableFrom(type))
            {
                members.Add(new ViewModelMember(property.Name, ViewModelMemberKind.Property, type));
            }
        }

        return members;
    }

    private static bool TryGeneric(Type type, Type openGeneric, out Type argument)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
            {
                argument = current.GetGenericArguments()[0];
                return true;
            }
        }

        argument = null!;
        return false;
    }
}
#endif
