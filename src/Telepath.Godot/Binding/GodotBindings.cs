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

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        Label label,
        Func<T, string> convert)
    {
        bindings.OneWay(source, value => label.Text = value, convert);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        Label label,
        IValueConverter<T, string> converter)
    {
        bindings.OneWay(source, value => label.Text = value, converter);
    }

    public static void BindText(this BindingSet bindings, Observable<string> source, RichTextLabel label)
    {
        bindings.OneWay(source, value => label.Text = value);
    }

    public static void BindText<T>(this BindingSet bindings, Observable<T> source, RichTextLabel label)
    {
        bindings.OneWay(source, value => label.Text = value?.ToString() ?? string.Empty);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        RichTextLabel label,
        Func<T, string> convert)
    {
        bindings.OneWay(source, value => label.Text = value, convert);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        RichTextLabel label,
        IValueConverter<T, string> converter)
    {
        bindings.OneWay(source, value => label.Text = value, converter);
    }

    public static void BindText(this BindingSet bindings, Observable<string> source, LineEdit edit)
    {
        bindings.OneWay(source, value => edit.Text = value);
    }

    public static void BindText(this BindingSet bindings, BindableReactiveProperty<string> source, LineEdit edit)
    {
        bindings.TwoWay(source, () => edit.Text, value => edit.Text = value, edit.OnTextChangedAsObservable());
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        LineEdit edit,
        Func<T, string> convert)
    {
        bindings.OneWay(source, value => edit.Text = value, convert);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        LineEdit edit,
        IValueConverter<T, string> converter)
    {
        bindings.OneWay(source, value => edit.Text = value, converter);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        LineEdit edit,
        Func<T, string> convert,
        Func<string, T> convertBack)
    {
        bindings.TwoWay(
            source,
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable(),
            convert,
            convertBack);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        LineEdit edit,
        ITwoWayValueConverter<T, string> converter)
    {
        bindings.TwoWay(
            source,
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable(),
            converter);
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

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        TextEdit edit,
        Func<T, string> convert)
    {
        bindings.OneWay(source, value => edit.Text = value, convert);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        Observable<T> source,
        TextEdit edit,
        IValueConverter<T, string> converter)
    {
        bindings.OneWay(source, value => edit.Text = value, converter);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        TextEdit edit,
        Func<T, string> convert,
        Func<string, T> convertBack)
    {
        bindings.TwoWay(
            source,
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable().Select(_ => edit.Text),
            convert,
            convertBack);
    }

    public static void BindText<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        TextEdit edit,
        ITwoWayValueConverter<T, string> converter)
    {
        bindings.TwoWay(
            source,
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable().Select(_ => edit.Text),
            converter);
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

    public static void BindToggle<T>(
        this BindingSet bindings,
        Observable<T> source,
        BaseButton button,
        Func<T, bool> convert)
    {
        button.ToggleMode = true;
        bindings.OneWay(source, value => button.ButtonPressed = value, convert);
    }

    public static void BindToggle<T>(
        this BindingSet bindings,
        Observable<T> source,
        BaseButton button,
        IValueConverter<T, bool> converter)
    {
        button.ToggleMode = true;
        bindings.OneWay(source, value => button.ButtonPressed = value, converter);
    }

    public static void BindToggle<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        BaseButton button,
        Func<T, bool> convert,
        Func<bool, T> convertBack)
    {
        button.ToggleMode = true;
        bindings.TwoWay(
            source,
            () => button.ButtonPressed,
            value => button.ButtonPressed = value,
            button.OnToggledAsObservable(),
            convert,
            convertBack);
    }

    public static void BindToggle<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        BaseButton button,
        ITwoWayValueConverter<T, bool> converter)
    {
        button.ToggleMode = true;
        bindings.TwoWay(
            source,
            () => button.ButtonPressed,
            value => button.ButtonPressed = value,
            button.OnToggledAsObservable(),
            converter);
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

    public static void BindValue(this BindingSet bindings, Observable<int> source, global::Godot.Range range)
    {
        bindings.OneWay(source, value => range.Value = value);
    }

    public static void BindValue(this BindingSet bindings, BindableReactiveProperty<int> source, global::Godot.Range range)
    {
        bindings.TwoWay(
            source,
            () => (int)range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable().Select(static value => (int)value));
    }

    public static void BindValue(this BindingSet bindings, Observable<float> source, global::Godot.Range range)
    {
        bindings.OneWay(source, value => range.Value = value);
    }

    public static void BindValue(this BindingSet bindings, BindableReactiveProperty<float> source, global::Godot.Range range)
    {
        bindings.TwoWay(
            source,
            () => (float)range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable().Select(static value => (float)value));
    }

    public static void BindValue<T>(
        this BindingSet bindings,
        Observable<T> source,
        global::Godot.Range range,
        Func<T, double> convert)
    {
        bindings.OneWay(source, value => range.Value = value, convert);
    }

    public static void BindValue<T>(
        this BindingSet bindings,
        Observable<T> source,
        global::Godot.Range range,
        IValueConverter<T, double> converter)
    {
        bindings.OneWay(source, value => range.Value = value, converter);
    }

    public static void BindValue<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        global::Godot.Range range,
        Func<T, double> convert,
        Func<double, T> convertBack)
    {
        bindings.TwoWay(
            source,
            () => range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable(),
            convert,
            convertBack);
    }

    public static void BindValue<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        global::Godot.Range range,
        ITwoWayValueConverter<T, double> converter)
    {
        bindings.TwoWay(
            source,
            () => range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable(),
            converter);
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

    public static void BindSelected<T>(
        this BindingSet bindings,
        Observable<T> source,
        OptionButton button,
        Func<T, long> convert)
    {
        bindings.OneWay(source, value => button.Selected = (int)value, convert);
    }

    public static void BindSelected<T>(
        this BindingSet bindings,
        Observable<T> source,
        OptionButton button,
        IValueConverter<T, long> converter)
    {
        bindings.OneWay(source, value => button.Selected = (int)value, converter);
    }

    public static void BindSelected<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        OptionButton button,
        Func<T, long> convert,
        Func<long, T> convertBack)
    {
        bindings.TwoWay(
            source,
            () => button.Selected,
            value => button.Selected = (int)value,
            button.OnItemSelectedAsObservable(),
            convert,
            convertBack);
    }

    public static void BindSelected<T>(
        this BindingSet bindings,
        BindableReactiveProperty<T> source,
        OptionButton button,
        ITwoWayValueConverter<T, long> converter)
    {
        bindings.TwoWay(
            source,
            () => button.Selected,
            value => button.Selected = (int)value,
            button.OnItemSelectedAsObservable(),
            converter);
    }

    public static void BindVisible(this BindingSet bindings, Observable<bool> source, CanvasItem node)
    {
        bindings.OneWay(source, value => node.Visible = value);
    }

    public static void BindVisible<T>(
        this BindingSet bindings,
        Observable<T> source,
        CanvasItem node,
        Func<T, bool> convert)
    {
        bindings.OneWay(source, value => node.Visible = value, convert);
    }

    public static void BindVisible<T>(
        this BindingSet bindings,
        Observable<T> source,
        CanvasItem node,
        IValueConverter<T, bool> converter)
    {
        bindings.OneWay(source, value => node.Visible = value, converter);
    }

    public static void BindDisabled(this BindingSet bindings, Observable<bool> source, BaseButton button)
    {
        bindings.OneWay(source, value => button.Disabled = value);
    }

    public static void BindDisabled<T>(
        this BindingSet bindings,
        Observable<T> source,
        BaseButton button,
        Func<T, bool> convert)
    {
        bindings.OneWay(source, value => button.Disabled = value, convert);
    }

    public static void BindDisabled<T>(
        this BindingSet bindings,
        Observable<T> source,
        BaseButton button,
        IValueConverter<T, bool> converter)
    {
        bindings.OneWay(source, value => button.Disabled = value, converter);
    }
}
