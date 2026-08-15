using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Marks a host-assembly Godot <c>Control</c> as a Telepath view.
/// </summary>
/// <typeparam name="TViewModel">The strongly typed ViewModel owned by the view.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TelepathViewAttribute<TViewModel> : Attribute
    where TViewModel : class, IViewModel;
