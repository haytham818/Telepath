#if TOOLS
using Godot;
using R3;
using Telepath.Godot.Editor;

namespace Telepath.Godot;

[Tool]
public partial class TelepathEditorPlugin : EditorPlugin
{
    private const string FrameProviderDispatcherName = "FrameProviderDispatcher";
    private const string FrameProviderDispatcherPath =
        "res://addons/Telepath/FrameProviderDispatcher.gd";
    private const string BindingDockScenePath =
        "res://addons/Telepath/Editor/TelepathBindingDock.tscn";

    private readonly GodotFramePump _frames = new();
    private TelepathBindingDock? _dock;

    public override void _EnterTree()
    {
        AlcUnloadHook.Register();

        // Leftover Autoload from a failed reload pins the previous ALC.
        RemoveAutoloadSingleton(FrameProviderDispatcherName);
        AddAutoloadSingleton(FrameProviderDispatcherName, FrameProviderDispatcherPath);

        SetProcess(true);
        SetPhysicsProcess(true);
        _frames.Start();

        var packed = GD.Load<PackedScene>(BindingDockScenePath)
            ?? throw new InvalidOperationException($"Missing dock scene '{BindingDockScenePath}'.");
        _dock = packed.Instantiate<TelepathBindingDock>();
        _dock.Attach(this);
        AddDock(_dock);
    }

    public override void _Process(double delta)
    {
        _frames.Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _frames.PhysicsProcess(delta);
    }

    public override void _ExitTree()
    {
        SetProcess(false);
        SetPhysicsProcess(false);
        _frames.Stop();
        UnloadDock();
        RemoveAutoloadSingleton(FrameProviderDispatcherName);
    }

    private void UnloadDock()
    {
        if (_dock is null)
        {
            return;
        }

        var dock = _dock;
        _dock = null;
        dock.Detach();

        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(dock))
        {
            return;
        }

        try
        {
            RemoveDock(dock);
        }
        catch (ObjectDisposedException)
        {
        }

        if (GodotObject.IsInstanceValid(dock))
        {
            dock.Free();
        }
    }
}
#endif
