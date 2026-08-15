using Godot;
using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps Godot controls onto <see cref="BindingExtensions"/> primitives.
/// </summary>
public static class GodotBindings
{
    public static void BindText(this BindingSet bindings, Observable<string> source, Label label)
    {
        bindings.OneWay(source, value => label.Text = value);
    }

    public static void BindText<T>(this BindingSet bindings, Observable<T> source, Label label)
    {
        bindings.OneWay(source, value => label.Text = value?.ToString() ?? string.Empty);
    }

    public static void BindText(this BindingSet bindings, Observable<string> source, RichTextLabel label)
    {
        bindings.OneWay(source, value => label.Text = value);
    }

    public static void BindText<T>(this BindingSet bindings, Observable<T> source, RichTextLabel label)
    {
        bindings.OneWay(source, value => label.Text = value?.ToString() ?? string.Empty);
    }

    public static void BindText(this BindingSet bindings, Observable<string> source, LineEdit edit)
    {
        bindings.OneWay(source, value => edit.Text = value);
    }

    public static void BindText(this BindingSet bindings, BindableReactiveProperty<string> source, LineEdit edit)
    {
        bindings.TwoWay(source, () => edit.Text, value => edit.Text = value, edit.OnTextChangedAsObservable());
    }

    public static void BindText(this BindingSet bindings, Observable<string> source, TextEdit edit)
    {
        bindings.OneWay(source, value => edit.Text = value);
    }

    public static void BindText(this BindingSet bindings, BindableReactiveProperty<string> source, TextEdit edit)
    {
        bindings.TwoWay(
            source,
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable().Select(_ => edit.Text));
    }

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

    public static void BindToggle(this BindingSet bindings, Observable<bool> source, BaseButton button)
    {
        button.ToggleMode = true;
        bindings.OneWay(source, value => button.ButtonPressed = value);
    }

    public static void BindToggle(this BindingSet bindings, BindableReactiveProperty<bool> source, BaseButton button)
    {
        button.ToggleMode = true;
        bindings.TwoWay(
            source,
            () => button.ButtonPressed,
            value => button.ButtonPressed = value,
            button.OnToggledAsObservable());
    }

    public static void BindValue(this BindingSet bindings, Observable<double> source, global::Godot.Range range)
    {
        bindings.OneWay(source, value => range.Value = value);
    }

    public static void BindValue(this BindingSet bindings, BindableReactiveProperty<double> source, global::Godot.Range range)
    {
        bindings.TwoWay(
            source,
            () => range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable());
    }

    public static void BindSelected(this BindingSet bindings, Observable<long> source, OptionButton button)
    {
        bindings.OneWay(source, value => button.Selected = (int)value);
    }

    public static void BindSelected(this BindingSet bindings, BindableReactiveProperty<long> source, OptionButton button)
    {
        bindings.TwoWay(
            source,
            () => button.Selected,
            value => button.Selected = (int)value,
            button.OnItemSelectedAsObservable());
    }

    public static void BindVisible(this BindingSet bindings, Observable<bool> source, CanvasItem node)
    {
        bindings.OneWay(source, value => node.Visible = value);
    }

    public static void BindDisabled(this BindingSet bindings, Observable<bool> source, BaseButton button)
    {
        bindings.OneWay(source, value => button.Disabled = value);
    }
}
