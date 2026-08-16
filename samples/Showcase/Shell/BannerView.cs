using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<BannerViewModel>]
public partial class BannerView : Control, IViewTransition
{
    [NodeInject("%Top")]
    private MarginContainer _bar = null!;

    public override partial void _Notification(int what);

    private BannerViewModel CreateViewModel() =>
        throw new InvalidOperationException("BannerView expects an injected ViewModel.");

    public Task PlayEnterAsync(CancellationToken cancellationToken)
    {
        var restY = _bar.Position.Y;
        _bar.Modulate = new Color(_bar.Modulate, 0);
        _bar.Position = new Vector2(_bar.Position.X, restY - 40);
        return PlayAsync(restY, 1f, cancellationToken);
    }

    public Task PlayExitAsync(CancellationToken cancellationToken) =>
        PlayAsync(_bar.Position.Y - 40, 0f, cancellationToken);

    private Task PlayAsync(float toY, float toAlpha, CancellationToken cancellationToken)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_bar, "modulate:a", toAlpha, 0.22);
        tween.TweenProperty(_bar, "position:y", toY, 0.22);
        return TweenTransition.PlayAsync(tween, cancellationToken);
    }
}
