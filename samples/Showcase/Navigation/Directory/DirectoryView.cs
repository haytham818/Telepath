using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<DirectoryViewModel>]
public partial class DirectoryView : Control, IViewTransition
{
    public override partial void _Notification(int what);

    private DirectoryViewModel CreateViewModel() =>
        throw new InvalidOperationException("DirectoryView expects an injected ViewModel.");

    public Task PlayEnterAsync(CancellationToken cancellationToken) =>
        FadeTransition.EnterAsync(this, cancellationToken);

    public Task PlayExitAsync(CancellationToken cancellationToken) =>
        FadeTransition.ExitAsync(this, cancellationToken);
}
