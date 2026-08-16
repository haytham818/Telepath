namespace Telepath.Godot;

/// <summary>
/// Binds an injected view control to a ViewModel member.
/// Requires <see cref="NodeInjectAttribute"/> on the same field or property.
/// Stack multiple attributes when one control needs more than one binding.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class BindToAttribute : Attribute
{
    public BindToAttribute(string member)
    {
        Member = member;
    }

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

    /// <summary>
    /// A <c>Telepath.Core.IValueConverter&lt;TSource, TTarget&gt;</c> (or two-way subtype)
    /// used to convert the ViewModel value. Must be a concrete type with a public
    /// parameterless constructor. Invalid on command bindings.
    /// </summary>
    public Type? Converter { get; set; }
}
