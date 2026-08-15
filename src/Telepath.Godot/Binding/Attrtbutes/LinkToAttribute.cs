namespace Telepath.Godot;

/// <summary>
/// Binds a view field or property to a ViewModel member via a Godot node path.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class LinkToAttribute : Attribute
{
    public LinkToAttribute(string nodePath, string member)
    {
        NodePath = nodePath;
        Member = member;
    }

    /// <summary>
    /// Godot node path, including unique-name syntax such as <c>%CountLabel</c>.
    /// </summary>
    public string NodePath { get; }

    /// <summary>
    /// ViewModel property or command name to bind.
    /// </summary>
    public string Member { get; }

    /// <summary>
    /// Overrides control-type inference. Leave <see cref="LinkKind.Auto"/> to infer.
    /// </summary>
    public LinkKind Kind { get; set; } = LinkKind.Auto;

    /// <summary>
    /// View field or property whose value is passed to a <c>ReactiveCommand&lt;T&gt;</c>
    /// when the linked button is pressed.
    /// </summary>
    public string? Parameter { get; set; }
}
