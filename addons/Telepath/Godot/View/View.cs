using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Non-generic Godot Control surface. Method table is in <c>View.GodotMethods.cs</c>
/// (library projects cannot use Godot.SourceGenerators: ScriptPath needs GodotProjectDir).
/// A generic script base is invisible to Godot's method list.
/// </summary>
public abstract partial class View : Control
{
    public override void _Ready()
    {
        base._Ready();
        HandleReady();
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        HandleEnterTree();
    }

    public override void _ExitTree()
    {
        HandleExitTree();
        base._ExitTree();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationPredelete)
        {
            HandlePredelete();
        }
    }

    protected abstract void HandleReady();
    protected abstract void HandleEnterTree();
    protected abstract void HandleExitTree();
    protected abstract void HandlePredelete();
}

/// <summary>
/// Control-based View. Creates or accepts a <typeparamref name="TViewModel"/> once,
/// attaches bindings on enter/ready, detaches on exit tree, and Disposes the
/// ViewModel only on <see cref="Node.NotificationPredelete"/>.
/// </summary>
/// <remarks>
/// The Godot script class must be non-generic (e.g. <c>CounterView : View&lt;CounterViewModel&gt;</c>).
/// Override <see cref="OnReady"/> / <see cref="OnBind"/>; do not skip <c>base._Ready</c>.
/// Do not <c>AddTo(this)</c> the ViewModel.
/// </remarks>
public abstract partial class View<TViewModel> : View
    where TViewModel : class, IViewModel
{
    private BindingSet? _bindings;

    /// <summary>
    /// Injected or created once via <see cref="CreateViewModel"/>. Not disposed on exit tree.
    /// </summary>
    public TViewModel? ViewModel { get; set; }

    /// <summary>Resolve child nodes. Called from <see cref="Node._Ready"/> before bind.</summary>
    protected virtual void OnReady()
    {
    }

    /// <summary>Called once when <see cref="ViewModel"/> is still null at ready.</summary>
    protected abstract TViewModel CreateViewModel();

    /// <summary>
    /// Wire ViewModel to nodes. Subscriptions must go into <paramref name="bindings"/>.
    /// Called again after re-enter tree.
    /// </summary>
    protected abstract void OnBind(TViewModel vm, BindingSet bindings);

    protected sealed override void HandleReady()
    {
        OnReady();
        EnsureViewModel();
        AttachBindings();
    }

    protected sealed override void HandleEnterTree()
    {
        if (ViewModel is not null && _bindings is null && IsNodeReady())
        {
            AttachBindings();
        }
    }

    protected sealed override void HandleExitTree()
    {
        DetachBindings();
    }

    protected sealed override void HandlePredelete()
    {
        DetachBindings();
        ViewModel?.Dispose();
        ViewModel = null;
    }

    private void EnsureViewModel()
    {
        ViewModel ??= CreateViewModel();
    }

    private void AttachBindings()
    {
        if (ViewModel is null || ViewModel.IsDisposed || _bindings is not null)
        {
            return;
        }

        var bindings = new BindingSet();
        OnBind(ViewModel, bindings);
        _bindings = bindings;
    }

    private void DetachBindings()
    {
        _bindings?.Dispose();
        _bindings = null;
    }
}
