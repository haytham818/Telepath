#if TOOLS
#nullable enable
using System.Linq;
using System.Reflection;

namespace Telepath.Godot.Editor;

internal static class EditorTypeScan
{
    public static IEnumerable<Type> SafeTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.OfType<Type>().ToArray();
            }

            foreach (var type in types)
            {
                yield return type;
            }
        }
    }
}
#endif
