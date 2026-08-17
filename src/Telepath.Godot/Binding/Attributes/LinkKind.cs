namespace Telepath.Godot;

/// <summary>
/// Binding kind for <see cref="BindToAttribute"/>. <see cref="Auto"/> infers from the control type.
/// </summary>
public enum LinkKind
{
    Auto = 0,
    Text,
    Command,
    Toggle,
    Value,
    Selected,
    Visible,
    Disabled,
    Items,
    View,
}
