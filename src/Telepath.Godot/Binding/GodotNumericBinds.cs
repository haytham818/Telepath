using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Implicit numeric conversion onto <see cref="GodotTargets.Value"/>.
/// </summary>
public static class GodotNumericBinds
{
    public static void Bind(
        this BindingSet bindings,
        Observable<int> source,
        BindingTarget<double> target)
    {
        bindings.Bind(source, target, static value => value);
    }

    public static void Bind(
        this BindingSet bindings,
        BindableReactiveProperty<int> source,
        BindingTarget<double> target)
    {
        bindings.Bind(source, target, static value => value, static value => (int)value);
    }

    public static void Bind(
        this BindingSet bindings,
        Observable<float> source,
        BindingTarget<double> target)
    {
        bindings.Bind(source, target, static value => value);
    }

    public static void Bind(
        this BindingSet bindings,
        BindableReactiveProperty<float> source,
        BindingTarget<double> target)
    {
        bindings.Bind(source, target, static value => value, static value => (float)value);
    }
}
