using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.Shell;

[TelepathView<DirectoryViewModel>]
public partial class DirectoryView : Control
{
    public override partial void _Notification(int what);

    private DirectoryViewModel CreateViewModel() =>
        throw new InvalidOperationException("DirectoryView expects an injected ViewModel.");
}
