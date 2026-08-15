namespace Telepath.SourceGenerator;

internal static class GeneratedMemberNames
{
    public static string FromField(string fieldName, string? explicitName)
    {
        if (!string.IsNullOrEmpty(explicitName))
        {
            return explicitName!;
        }

        var name = fieldName.StartsWith("_", StringComparison.Ordinal)
            ? fieldName.Substring(1)
            : fieldName;

        if (name.Length == 0)
        {
            return fieldName;
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    public static string FromBindableMethod(string methodName, string? explicitName)
    {
        if (!string.IsNullOrEmpty(explicitName))
        {
            return explicitName!;
        }

        foreach (var prefix in new[] { "Get", "Compute", "Format" })
        {
            if (HasPrefix(methodName, prefix))
            {
                return methodName.Substring(prefix.Length);
            }
        }

        return methodName;
    }

    public static string FromCommandMethod(string methodName, string? explicitName)
    {
        if (!string.IsNullOrEmpty(explicitName))
        {
            return explicitName!;
        }

        var name = HasPrefix(methodName, "On")
            ? methodName.Substring(2)
            : methodName;

        return name.EndsWith("Command", StringComparison.Ordinal)
            ? name
            : name + "Command";
    }

    public static string BackingField(string propertyName)
        => "__telepath_" + propertyName;

    private static bool HasPrefix(string name, string prefix)
    {
        return name.Length > prefix.Length
            && name.StartsWith(prefix, StringComparison.Ordinal)
            && char.IsUpper(name[prefix.Length]);
    }
}
