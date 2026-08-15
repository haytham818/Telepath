using System.Collections.Immutable;
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

    public static IEnumerable<AttributeData> GetAttributes(ISymbol symbol, string metadataName)
    {
        foreach (var candidate in symbol.GetAttributes())
        {
            if (candidate.AttributeClass is not null
                && HasMetadataName(candidate.AttributeClass, metadataName))
            {
                yield return candidate;
            }
        }
    }

    public static bool TryGetAttribute(
        ISymbol symbol,
        string metadataName,
        out AttributeData attribute)
    {
        foreach (var candidate in GetAttributes(symbol, metadataName))
        {
            attribute = candidate;
            return true;
        }

        attribute = null!;
        return false;
    }

    public static bool TryGetNamedEnum(
        AttributeData attribute,
        string name,
        string metadataName,
        out int value)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != name)
            {
                continue;
            }

            if (argument.Value.Type is INamedTypeSymbol enumType
                && HasMetadataName(enumType, metadataName)
                && argument.Value.Value is not null)
            {
                value = Convert.ToInt32(argument.Value.Value);
                return true;
            }

            break;
        }

        value = 0;
        return false;
    }

    public static string? GetNamedString(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is string value)
            {
                return value;
            }
        }

        return null;
    }

    public static ImmutableArray<string> GetConstructorStringArray(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return ImmutableArray<string>.Empty;
        }

        var argument = attribute.ConstructorArguments[0];
        if (argument.Kind == TypedConstantKind.Array)
        {
            return argument.Values
                .Select(static value => value.Value as string)
                .Where(static value => value is not null)
                .ToImmutableArray()!;
        }

        return argument.Value is string single
            ? ImmutableArray.Create(single)
            : ImmutableArray<string>.Empty;
    }

    public static bool IsObservableOfBool(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named
            && named.TypeArguments.Length == 1
            && named.TypeArguments[0].SpecialType == SpecialType.System_Boolean
            && HasMetadataName(named.OriginalDefinition, "R3.Observable`1");
    }
}
