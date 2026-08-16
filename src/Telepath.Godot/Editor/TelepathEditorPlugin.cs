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
    private const string BindingDockScenePath =
        "res://addons/Telepath/Editor/TelepathBindingDock.tscn";

    private TelepathBindingDock? _dock;

    public override void _EnterTree()
    {
        AddAutoloadSingleton(FrameProviderDispatcherName, FrameProviderDispatcherPath);
        var packed = GD.Load<PackedScene>(BindingDockScenePath)
            ?? throw new InvalidOperationException($"Missing dock scene '{BindingDockScenePath}'.");
        _dock = packed.Instantiate<TelepathBindingDock>();
        _dock.Attach(this);
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
