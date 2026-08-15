using Godot;
using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps Godot control properties onto <see cref="BindingTarget{T}"/>.
/// </summary>
public static class GodotTargets
{
    public static BindingTarget<string> Text(this Label label)
        => BindingTarget<string>.OneWay(value => label.Text = value);

    public static BindingTarget<string> Text(this RichTextLabel label)
        => BindingTarget<string>.OneWay(value => label.Text = value);

    public static BindingTarget<string> Text(this LineEdit edit)
        => BindingTarget<string>.TwoWay(
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable());

    public static BindingTarget<string> Text(this TextEdit edit)
        => BindingTarget<string>.TwoWay(
            () => edit.Text,
            value => edit.Text = value,
            edit.OnTextChangedAsObservable().Select(_ => edit.Text));

    public static BindingTarget<bool> Toggle(this BaseButton button)
    {
        button.ToggleMode = true;
        return BindingTarget<bool>.TwoWay(
            () => button.ButtonPressed,
            value => button.ButtonPressed = value,
            button.OnToggledAsObservable());
    }

    public static BindingTarget<double> Value(this global::Godot.Range range)
        => BindingTarget<double>.TwoWay(
            () => range.Value,
            value => range.Value = value,
            range.OnValueChangedAsObservable());

    public static BindingTarget<long> Selected(this OptionButton button)
        => BindingTarget<long>.TwoWay(
            () => button.Selected,
            value => button.Selected = (int)value,
            button.OnItemSelectedAsObservable());

    public static BindingTarget<long> Selected(this ItemList list)
        => BindingTarget<long>.TwoWay(
            () =>
            {
                var selected = list.GetSelectedItems();
                return selected.Length == 0 ? -1 : selected[0];
            },
            value =>
            {
                if (value < 0 || value >= list.ItemCount)
                {
                    list.DeselectAll();
                    return;
                }

                list.Select((int)value);
            },
            Observable.FromEvent<ItemList.ItemSelectedEventHandler, long>(
                h => new ItemList.ItemSelectedEventHandler(h),
                h => list.ItemSelected += h,
                h => list.ItemSelected -= h));

    public static BindingTarget<bool> Visible(this CanvasItem node)
        => BindingTarget<bool>.OneWay(value => node.Visible = value);

    public static BindingTarget<bool> Disabled(this BaseButton button)
        => BindingTarget<bool>.OneWay(value => button.Disabled = value);
}
