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
        _frames?.Stop();
        _frames = null;
        SetProcess(false);
        SetPhysicsProcess(false);
        __telepathViewLifecycle?.Release();
        __telepathViewLifecycle = null;
        _plugin = null;
    }

    public void NotifySelectionChanged()
    {
        if (ViewModel is BindingDockViewModel viewModel)
        {
            viewModel.RefreshSelection();
        }
    }

    public void OnBeforeSerialize()
    {
        // ManagedCallable snapshot already happened. Do not `-=` Godot signals here.
        _frames?.Stop();
        _frames = null;
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    public void OnAfterDeserialize()
    {
        AlcUnloadHook.Register();
        StaleCallableCleanup.DropInvalid(EditorInterface.Singleton.GetSelection());
        StaleCallableCleanup.DropInvalidTree(this);
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
}
#endif
