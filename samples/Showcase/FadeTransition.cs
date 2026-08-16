using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

internal static class FadeTransition
{
    private const double Duration = 0.18;

    public static Task EnterAsync(Control view, CancellationToken cancellationToken)
    {
        view.Modulate = new Color(view.Modulate, 0);
        return FadeToAsync(view, 1f, cancellationToken);
    }

    public static Task ExitAsync(Control view, CancellationToken cancellationToken) =>
        FadeToAsync(view, 0f, cancellationToken);

    private static Task FadeToAsync(Control view, float alpha, CancellationToken cancellationToken)
    {
        var tween = view.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(view, "modulate:a", alpha, Duration);
        return TweenTransition.PlayAsync(tween, cancellationToken);
    }
}
