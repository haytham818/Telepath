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
}
