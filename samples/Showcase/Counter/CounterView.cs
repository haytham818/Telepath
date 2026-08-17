using Godot;
using Telepath.Godot;
using Telepath.Showcase;

namespace Telepath.Showcase;

[TelepathView<CounterViewModel>]
public partial class CounterView : Control, IViewTransition
{
    public override partial void _Notification(int what);

    private CounterViewModel CreateViewModel() => new();

    public Task PlayEnterAsync(CancellationToken cancellationToken) =>
        FadeTransition.EnterAsync(this, cancellationToken);

    public Task PlayExitAsync(CancellationToken cancellationToken) =>
        FadeTransition.ExitAsync(this, cancellationToken);
}
