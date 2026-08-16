using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.CounterApp;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    [NodeInject("%CountLabel")]
    [BindTo(nameof(CounterViewModel.Count), Converter = typeof(CountTextConverter))]
    private Label _countLabel = null!;

    [NodeInject("%DecrementButton")]
    [BindTo(nameof(CounterViewModel.DecrementCommand))]
    private Button _decrementButton = null!;

    [NodeInject("%ResetButton")]
    [BindTo(nameof(CounterViewModel.ResetCommand))]
    private Button _resetButton = null!;

    [NodeInject("%IncrementButton")]
    [BindTo(nameof(CounterViewModel.IncrementCommand))]
    private Button _incrementButton = null!;

    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
