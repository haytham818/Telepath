using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.CounterApp;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    [LinkTo("%CountLabel", nameof(CounterViewModel.CountText))]
    private Label _countLabel = null!;

    [LinkTo("%DecrementButton", nameof(CounterViewModel.Decrement))]
    private Button _decrementButton = null!;

    [LinkTo("%ResetButton", nameof(CounterViewModel.Reset))]
    private Button _resetButton = null!;

    [LinkTo("%IncrementButton", nameof(CounterViewModel.Increment))]
    private Button _incrementButton = null!;

    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
