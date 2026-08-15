using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.CounterApp;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    private Label _countLabel = null!;
    private Button _decrementButton = null!;
    private Button _resetButton = null!;
    private Button _incrementButton = null!;

    public override partial void _Notification(int what);

    private void OnReady()
    {
        _countLabel = GetNode<Label>("%CountLabel");
        _decrementButton = GetNode<Button>("%DecrementButton");
        _resetButton = GetNode<Button>("%ResetButton");
        _incrementButton = GetNode<Button>("%IncrementButton");
    }

    private CounterViewModel CreateViewModel() => new();

    private void OnBind(CounterViewModel vm, BindingSet bindings)
    {
        bindings.BindLabel(vm.CountText, _countLabel);
        bindings.BindCommand(vm.Decrement, _decrementButton);
        bindings.BindCommand(vm.Reset, _resetButton);
        bindings.BindCommand(vm.Increment, _incrementButton);
    }
}
