#if TOOLS
using Godot;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

[Tool]
[TelepathView<BindingDockViewModel>]
public partial class TelepathBindingDock : EditorDock
{
    private EditorPlugin? _plugin;

    public override partial void _Notification(int what);

    public void Attach(EditorPlugin plugin)
    {
        _plugin = plugin;
    }

    private BindingDockViewModel CreateViewModel()
    {
        var viewModel = new BindingDockViewModel();
        if (_plugin is not null)
        {
            viewModel.Connect(_plugin);
        }

        return viewModel;
    }
}
#endif
