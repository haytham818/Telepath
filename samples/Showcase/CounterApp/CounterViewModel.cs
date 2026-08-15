using System;
using R3;
using Telepath.Core;

namespace Telepath.Showcase.CounterApp;

public sealed partial class CounterViewModel : ViewModel
{
    private readonly int _min;
    private readonly int _max;
    private readonly int _initial;

    [Bindable]
    private int _count;

    public CounterViewModel(int initial = 0, int min = 0, int max = 10)
    {
        if (min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "min must be <= max.");
        }

        _min = min;
        _max = max;
        _initial = initial;
        _count = Math.Clamp(initial, min, max);
    }

    [Bindable(nameof(Count))]
    private string GetCountText(int count) => $"Count: {count}";

    [Command(CanExecute = nameof(CanIncrement))]
    private void OnIncrement() => Count.Value++;

    private Observable<bool> CanIncrement() => Count.Select(c => c < _max);

    [Command(CanExecute = nameof(CanDecrement))]
    private void OnDecrement() => Count.Value--;

    private Observable<bool> CanDecrement() => Count.Select(c => c > _min);

    [Command(CanExecute = nameof(CanReset))]
    private void OnReset() => Count.Value = _initial;

    private Observable<bool> CanReset() => Count.Select(c => c != _initial);
}
