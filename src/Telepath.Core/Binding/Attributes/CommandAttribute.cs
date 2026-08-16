namespace Telepath.Core;

/// <summary>
/// Marks a method as the execute body of a generated <c>ReactiveCommand</c>.
/// The method may return <c>void</c>, <c>Task</c>, or <c>ValueTask</c>, and may
/// take an optional trailing <c>CancellationToken</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// Name of an <c>Observable&lt;bool&gt;</c> property or parameterless method used as CanExecute.
    /// </summary>
    public string? CanExecute { get; set; }

    /// <summary>
    /// Overrides the generated command property name.
    /// </summary>
    public string? Name { get; set; }
}
