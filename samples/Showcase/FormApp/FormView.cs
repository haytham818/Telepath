using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.FormApp;

[TelepathView<FormViewModel>]
public partial class FormView : Control
{
    public override partial void _Notification(int what);

    private FormViewModel CreateViewModel() => new();
}
