#if TOOLS
using Godot;

namespace Telepath.Godot;

[Tool]
public partial class TelepathEditorPlugin : EditorPlugin
{
    private const string FrameProviderDispatcherName = "FrameProviderDispatcher";
    private const string FrameProviderDispatcherPath =
        "res://addons/Telepath/Godot/R3/FrameProviderDispatcher.cs";

    public override void _EnterTree()
    {
        AddAutoloadSingleton(FrameProviderDispatcherName, FrameProviderDispatcherPath);
    }

    public override void _ExitTree()
    {
        RemoveAutoloadSingleton(FrameProviderDispatcherName);
    }
}
#endif
