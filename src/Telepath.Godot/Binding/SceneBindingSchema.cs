using Godot;
using GodotArray = Godot.Collections.Array;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Telepath.Godot;

/// <summary>
/// Reads and writes <see cref="SceneBindingEntry"/> lists as node metadata.
/// </summary>
public static class SceneBindingSchema
{
    public const string MetaKey = "telepath_bindings";

    private const string PathKey = "path";
    private const string MemberKey = "member";
    private const string KindKey = "kind";
    private const string ConverterKey = "converter";
    private const string ParameterKey = "parameter";
    private const string ItemViewKey = "item_view";
    private const string ItemSceneKey = "item_scene";

    public static IReadOnlyList<SceneBindingEntry> Read(Node view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!view.HasMeta(MetaKey))
        {
            return [];
        }

        var meta = view.GetMeta(MetaKey);
        if (meta.VariantType is Variant.Type.Nil)
        {
            return [];
        }

        var array = meta.AsGodotArray();
        var entries = new List<SceneBindingEntry>(array.Count);
        foreach (var item in array)
        {
            if (item.VariantType is Variant.Type.Nil)
            {
                continue;
            }

            entries.Add(FromDictionary(item.AsGodotDictionary()));
        }

        return entries;
    }

    public static void Write(Node view, IReadOnlyList<SceneBindingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            if (view.HasMeta(MetaKey))
            {
                view.RemoveMeta(MetaKey);
            }

            return;
        }

        view.SetMeta(MetaKey, Encode(entries));
    }

    public static GodotArray Encode(IReadOnlyList<SceneBindingEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var array = new GodotArray();
        foreach (var entry in entries)
        {
            array.Add(ToDictionary(entry));
        }

        return array;
    }

    public static SceneBindingEntry FromDictionary(GodotDictionary dict)
    {
        ArgumentNullException.ThrowIfNull(dict);
        return new SceneBindingEntry
        {
            Path = GetString(dict, PathKey),
            Member = GetString(dict, MemberKey),
            Kind = ParseKind(GetVariant(dict, KindKey)),
            Converter = EmptyToNull(GetString(dict, ConverterKey)),
            Parameter = EmptyToNull(GetString(dict, ParameterKey)),
            ItemView = EmptyToNull(GetString(dict, ItemViewKey)),
            ItemScene = EmptyToNull(GetString(dict, ItemSceneKey)),
        };
    }

    public static GodotDictionary ToDictionary(SceneBindingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var dict = new GodotDictionary
        {
            { PathKey, entry.Path },
            { MemberKey, entry.Member },
            { KindKey, entry.Kind.ToString() },
        };

        AddOptional(dict, ConverterKey, entry.Converter);
        AddOptional(dict, ParameterKey, entry.Parameter);
        AddOptional(dict, ItemViewKey, entry.ItemView);
        AddOptional(dict, ItemSceneKey, entry.ItemScene);
        return dict;
    }

    private static void AddOptional(GodotDictionary dict, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            dict[key] = value;
        }
    }

    private static Variant GetVariant(GodotDictionary dict, string key)
        => dict.ContainsKey(key) ? dict[key] : default;

    private static string GetString(GodotDictionary dict, string key)
    {
        if (!dict.ContainsKey(key))
        {
            return "";
        }

        var value = dict[key];
        return value.VariantType is Variant.Type.Nil ? "" : value.AsString();
    }

    private static LinkKind ParseKind(Variant value)
    {
        if (value.VariantType is Variant.Type.Nil)
        {
            return LinkKind.Auto;
        }

        if (value.VariantType is Variant.Type.Int)
        {
            var raw = value.AsInt32();
            return Enum.IsDefined(typeof(LinkKind), raw) ? (LinkKind)raw : LinkKind.Auto;
        }

        var text = value.AsString();
        return Enum.TryParse(text, ignoreCase: true, out LinkKind kind) ? kind : LinkKind.Auto;
    }

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
