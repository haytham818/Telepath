using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.CounterApp;

public partial class CounterView : View<CounterViewModel>
{
    private Label _countLabel = null!;
    private Button _decrementButton = null!;
    private Button _resetButton = null!;
    private Button _incrementButton = null!;

    protected override void OnReady()
    {
        _countLabel = GetNode<Label>("%CountLabel");
        _decrementButton = GetNode<Button>("%DecrementButton");
        _resetButton = GetNode<Button>("%ResetButton");
        _incrementButton = GetNode<Button>("%IncrementButton");
    }

    protected override CounterViewModel CreateViewModel() => new();

    protected override void OnBind(CounterViewModel vm, BindingSet bindings)
    {
        bindings.BindLabel(vm.CountText, _countLabel);
        bindings.BindCommand(vm.Decrement, _decrementButton);
        bindings.BindCommand(vm.Reset, _resetButton);
        bindings.BindCommand(vm.Increment, _incrementButton);
    }
}
