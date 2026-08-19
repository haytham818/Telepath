#if TOOLS
#nullable enable
using System.IO;
using System.Reflection;
using Godot;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

internal static class ViewScriptResolver
{
    public static Node? FindTelepathView(Node? node)
    {
        while (node is not null)
        {
            if (TryResolve(node, out _, out _))
            {
                return node;
            }

            node = node.GetParent();
        }

        return null;
    }

    public static bool TryResolve(Node node, out Type viewType, out Type viewModelType)
    {
        viewType = null!;
        viewModelType = null!;
        var script = node.GetScript().AsGodotObject() as CSharpScript;
        if (script is null)
        {
            return false;
        }

        var className = Path.GetFileNameWithoutExtension(script.ResourcePath);
        if (string.IsNullOrWhiteSpace(className))
        {
            return false;
        }

        foreach (var type in EditorTypeScan.SafeTypes())
        {
            if (type.Name != className)
            {
                continue;
            }

            if (!TryGetViewModel(type, out viewModelType))
            {
                continue;
            }

            viewType = type;
            return true;
        }

        return false;
    }

    public static bool TryGetViewModel(Type viewType, out Type viewModelType)
    {
        foreach (var attribute in viewType.GetCustomAttributes(inherit: false))
        {
            var attributeType = attribute.GetType();
            if (!attributeType.IsGenericType
                || attributeType.GetGenericTypeDefinition() != typeof(TelepathViewAttribute<>))
            {
                continue;
            }

            viewModelType = attributeType.GetGenericArguments()[0];
            return true;
        }

        viewModelType = null!;
        return false;
    }
}
#endif
