using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<ShellViewModel>]
public partial class ShellView : Control
{
    [NodeInject("%Content")]
    private Control _content = null!;

    [NodeInject("%Overlay")]
    private Control _overlay = null!;

    public override partial void _Notification(int what);

    private ShellViewModel CreateViewModel() => new();

    private void OnBind(ShellViewModel vm, BindingSet bindings)
        => vm.BindPresentation(bindings, _content, _overlay);
}
