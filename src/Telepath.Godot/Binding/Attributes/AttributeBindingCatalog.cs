using System.Reflection;

namespace Telepath.Godot;

/// <summary>
/// Reflects <see cref="BindToAttribute"/> declarations on a host view type
/// so the editor can show them as read-only bindings.
/// </summary>
public static class AttributeBindingCatalog
{
    public static IReadOnlyList<SceneBindingEntry> Read(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        var entries = new List<SceneBindingEntry>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        foreach (var member in viewType.GetMembers(flags))
        {
            var inject = member.GetCustomAttribute<NodeInjectAttribute>();
            if (inject is null)
            {
                continue;
            }

            foreach (var bind in member.GetCustomAttributes<BindToAttribute>())
            {
                entries.Add(new SceneBindingEntry
                {
                    Path = inject.NodePath,
                    Member = bind.Member,
                    Kind = bind.Kind,
                    Converter = bind.Converter?.FullName,
                    Parameter = bind.Parameter,
                    ItemView = bind.ItemView?.FullName,
                    ItemScene = bind.ItemScene,
                });
            }
        }

        return entries;
    }
}
