using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<ConfirmViewModel>]
public partial class ConfirmView : Control, IViewTransition, IViewCoverTransition
{
    [NodeInject("%Dimmer")]
    private ColorRect _dimmer = null!;

    [NodeInject("%Panel")]
    private PanelContainer _panel = null!;

    public override partial void _Notification(int what);

    private ConfirmViewModel CreateViewModel() =>
        throw new InvalidOperationException("ConfirmView expects an injected ViewModel.");

    public Task PlayEnterAsync(CancellationToken cancellationToken)
    {
        _dimmer.Modulate = new Color(_dimmer.Modulate, 0);
        _panel.Modulate = new Color(_panel.Modulate, 0);
        _panel.PivotOffset = _panel.Size / 2f;
        _panel.Scale = new Vector2(0.94f, 0.94f);
        return PlayAsync(toAlpha: 1f, toScale: Vector2.One, cancellationToken);
    }

    public Task PlayExitAsync(CancellationToken cancellationToken)
    {
        _panel.PivotOffset = _panel.Size / 2f;
        return PlayAsync(toAlpha: 0f, toScale: new Vector2(0.94f, 0.94f), cancellationToken);
    }

    public Task PlayCoverAsync(CancellationToken cancellationToken) =>
        FadeTransition.CoverAsync(_panel, cancellationToken);

    public Task PlayUncoverAsync(CancellationToken cancellationToken) =>
        FadeTransition.UncoverAsync(_panel, cancellationToken);

    private Task PlayAsync(float toAlpha, Vector2 toScale, CancellationToken cancellationToken)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_dimmer, "modulate:a", toAlpha, 0.18);
        tween.TweenProperty(_panel, "modulate:a", toAlpha, 0.2);
        tween.TweenProperty(_panel, "scale", toScale, 0.2);
        return TweenTransition.PlayAsync(tween, cancellationToken);
    }
}
