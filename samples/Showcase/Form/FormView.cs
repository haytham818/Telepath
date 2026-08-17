using Godot;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<FormViewModel>]
public partial class FormView : Control
{
    public override partial void _Notification(int what);

    private FormViewModel CreateViewModel() => new();
}
