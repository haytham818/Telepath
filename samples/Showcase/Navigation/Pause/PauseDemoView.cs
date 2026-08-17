using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<PauseDemoViewModel>]
public partial class PauseDemoView : Control, IViewCoverTransition
{
    public override partial void _Notification(int what);

    private PauseDemoViewModel CreateViewModel() =>
        throw new InvalidOperationException("PauseDemoView expects an injected ViewModel.");

    public Task PlayCoverAsync(CancellationToken cancellationToken) =>
        FadeTransition.CoverAsync(this, cancellationToken);

    public Task PlayUncoverAsync(CancellationToken cancellationToken) =>
        FadeTransition.UncoverAsync(this, cancellationToken);
}
