using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<PauseDemoViewModel>]
public partial class PauseDemoView : Control
{
    public override partial void _Notification(int what);

    private PauseDemoViewModel CreateViewModel() =>
        throw new InvalidOperationException("PauseDemoView expects an injected ViewModel.");
}
