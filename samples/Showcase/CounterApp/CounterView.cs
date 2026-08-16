using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.CounterApp;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control
{
    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();
}
