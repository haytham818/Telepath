namespace Telepath.Godot;

/// <summary>
/// Injects a Godot node into a view field or property via <c>GetNode</c>.
/// Pair with <see cref="BindToAttribute"/> for declarative bindings, or use alone
/// when wiring in <c>OnBind</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class NodeInjectAttribute : Attribute
{
    public NodeInjectAttribute(string nodePath)
    {
        NodePath = nodePath;
    }

    /// <summary>
    /// Godot node path, including unique-name syntax such as <c>%CountLabel</c>.
    /// </summary>
    public string NodePath { get; }
}
