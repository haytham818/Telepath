using System;
using R3;
using Telepath.Core;

namespace Telepath.Showcase.CounterApp;

/// <summary>
/// Real-world ViewModel shape: expose R3 properties/commands, register them with
/// <see cref="ViewModel.Track{T}"/>, derive UI text and CanExecute from the model.
/// No Godot types here — platform-agnostic Core usage only.
/// </summary>
public sealed class CounterViewModel : ViewModel
{
    public BindableReactiveProperty<int> Count { get; }

    /// <summary>Derived display text; updates whenever <see cref="Count"/> changes.</summary>
    public BindableReactiveProperty<string> CountText { get; }

    public ReactiveCommand Increment { get; }
    public ReactiveCommand Decrement { get; }
    public ReactiveCommand Reset { get; }

    public CounterViewModel(int initial = 0, int min = 0, int max = 10)
    {
        if (min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "min must be <= max.");
        }

        initial = Math.Clamp(initial, min, max);

        Count = Track(new BindableReactiveProperty<int>(initial));

        CountText = Track(
            Count.Select(c => $"Count: {c}")
                .ToBindableReactiveProperty($"Count: {initial}"));

        Increment = Track(
            Count.Select(c => c < max)
                .ToReactiveCommand(_ => Count.Value++));

        Decrement = Track(
            Count.Select(c => c > min)
                .ToReactiveCommand(_ => Count.Value--));

        Reset = Track(
            Count.Select(c => c != initial)
                .ToReactiveCommand(_ => Count.Value = initial));
    }
}
