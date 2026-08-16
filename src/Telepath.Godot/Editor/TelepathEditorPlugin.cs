#if TOOLS
using Godot;
using Telepath.Godot.Editor;

namespace Telepath.Godot;

[Tool]
public partial class TelepathEditorPlugin : EditorPlugin
{
    private const string FrameProviderDispatcherName = "FrameProviderDispatcher";
    private const string FrameProviderDispatcherPath =
        "res://addons/Telepath/FrameProviderDispatcher.cs";

    private TelepathBindingDock? _dock;

    public override void _EnterTree()
    {
        AddAutoloadSingleton(FrameProviderDispatcherName, FrameProviderDispatcherPath);
        _dock = new TelepathBindingDock(this);
        AddDock(_dock);
    }

    public override void _ExitTree()
    {
        if (_dock is not null)
        {
            RemoveDock(_dock);
            _dock.QueueFree();
            _dock = null;
        }

        RemoveAutoloadSingleton(FrameProviderDispatcherName);
    }
}
#endif
