using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<AboutViewModel>]
public partial class AboutView : Control, IViewTransition
{
    [NodeInject("%Transition")]
    private AnimationPlayer _transition = null!;

    [NodeInject("%Panel")]
    private PanelContainer _panel = null!;

    public override partial void _Notification(int what);

    private AboutViewModel CreateViewModel() =>
        throw new InvalidOperationException("AboutView expects an injected ViewModel.");

    public Task PlayEnterAsync(CancellationToken cancellationToken)
    {
        Modulate = new Color(Modulate, 0);
        _panel.PivotOffset = _panel.Size / 2f;
        return AnimationPlayerTransition.PlayAsync(_transition, "enter", cancellationToken);
    }

    public Task PlayExitAsync(CancellationToken cancellationToken)
    {
        _panel.PivotOffset = _panel.Size / 2f;
        return AnimationPlayerTransition.PlayAsync(_transition, "exit", cancellationToken);
    }
}
