#if TOOLS
using Godot;
using R3;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

[Tool]
[TelepathView<BindingDockViewModel>]
public partial class TelepathBindingDock : EditorDock, ISerializationListener
{
    private EditorPlugin? _plugin;
    private GodotFramePump? _frames;

    public override partial void _Notification(int what);

    public override void _Process(double delta)
    {
        _frames?.Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _frames?.PhysicsProcess(delta);
    }

    public void Attach(EditorPlugin plugin)
    {
        _plugin = plugin;
        AlcUnloadHook.Register();
        EnsurePump();
    }

    public void Detach()
    {
        SuspendForReload();
        _plugin = null;
    }

    public void OnBeforeSerialize()
    {
        // Called by Godot before ALC unload. _ExitTree is not.
        SuspendForReload();
    }

    public void OnAfterDeserialize()
    {
        AlcUnloadHook.Register();
        EnsurePump();
        __telepathViewLifecycle = null;
        if (IsInsideTree() && IsNodeReady())
        {
            __TelepathViewLifecycle.HandleNotification((int)NotificationReady);
        }
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

    private void EnsurePump()
    {
        _frames ??= new GodotFramePump();
        _frames.Start();
        SetProcess(true);
        SetPhysicsProcess(true);
    }

    private void SuspendForReload()
    {
        _frames?.Stop();
        _frames = null;
        SetProcess(false);
        SetPhysicsProcess(false);
        BindingDockViewModel.DisconnectActiveSelection();
        __telepathViewLifecycle?.Release();
        __telepathViewLifecycle = null;
    }
}
#endif
