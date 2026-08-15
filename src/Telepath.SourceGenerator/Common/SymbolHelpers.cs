using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal static class SymbolHelpers
{
    public static IEnumerable<IMethodSymbol> GetDeclaredInstanceMethods(
        INamedTypeSymbol type,
        string name)
    {
        return type.GetMembers(name)
            .OfType<IMethodSymbol>()
            .Where(static method =>
                !method.IsStatic
                && method.MethodKind == MethodKind.Ordinary);
    }

    public static bool ImplementsInterface(ITypeSymbol type, string metadataName)
    {
        return type is INamedTypeSymbol namedType
            && namedType.AllInterfaces.Any(interfaceType => HasMetadataName(interfaceType, metadataName));
    }

    public static bool IsOrInheritsFrom(INamedTypeSymbol type, string metadataName)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (HasMetadataName(current, metadataName))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasMetadataName(ITypeSymbol type, string metadataName)
    {
        var separator = metadataName.LastIndexOf('.');
        return type.MetadataName == metadataName.Substring(separator + 1)
            && type.ContainingNamespace.ToDisplayString()
                == metadataName.Substring(0, separator);
    }

    public static string SanitizeHintName(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }
}
