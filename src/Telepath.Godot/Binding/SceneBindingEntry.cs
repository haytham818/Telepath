namespace Telepath.Godot;

/// <summary>
/// One control-to-ViewModel link stored on a view node's metadata.
/// </summary>
public sealed class SceneBindingEntry
{
    public string Path { get; init; } = "";

    public string Member { get; init; } = "";

    public LinkKind Kind { get; init; } = LinkKind.Auto;

    public string? Converter { get; init; }

    public string? Parameter { get; init; }

    public string? ItemView { get; init; }

    public string? ItemScene { get; init; }
}
