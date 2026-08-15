namespace Telepath.Core;

/// <summary>
/// Marks a field as a source <c>BindableReactiveProperty</c>, or a method as a derived one.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class BindableAttribute : Attribute
{
    public BindableAttribute()
        : this([])
    {
    }

    public BindableAttribute(params string[] from)
    {
        From = from ?? [];
    }

    /// <summary>
    /// Source members for a derived bindable. Empty when this attribute is on a field.
    /// </summary>
    public string[] From { get; }

    /// <summary>
    /// Overrides the generated property name.
    /// </summary>
    public string? Name { get; set; }
}
