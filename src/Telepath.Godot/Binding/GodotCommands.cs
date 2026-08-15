using Godot;
using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps Godot input signals onto <see cref="BindingExtensions.BindCommand"/>.
/// </summary>
public static class GodotCommands
{
    public static void BindCommand(this BindingSet bindings, ReactiveCommand command, BaseButton button)
    {
        bindings.BindCommand(
            command,
            button.OnPressedAsObservable(),
            canExecute => button.Disabled = !canExecute);
    }

    public static void BindCommand<T>(
        this BindingSet bindings,
        ReactiveCommand<T> command,
        BaseButton button,
        Func<T> getParameter)
    {
        ArgumentNullException.ThrowIfNull(getParameter);
        bindings.BindCommand(
            command,
            button.OnPressedAsObservable().Select(_ => getParameter()),
            canExecute => button.Disabled = !canExecute);
    }

    public static void BindCommand(this BindingSet bindings, ReactiveCommand<string> command, LineEdit edit)
    {
        bindings.BindCommand(command, edit.OnTextSubmittedAsObservable());
    }

    public static void BindCommand(this BindingSet bindings, ReactiveCommand<long> command, OptionButton button)
    {
        bindings.BindCommand(
            command,
            button.OnItemSelectedAsObservable(),
            canExecute => button.Disabled = !canExecute);
    }
}
