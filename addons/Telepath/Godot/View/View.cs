using Godot;
using Godot.NativeInterop;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Non-generic Godot Control surface with a hand-written method table.
/// This file is not a Godot script (the View/ directory is <c>.gdignore</c>d);
/// only concrete views like CounterView are attached to scenes.
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

#pragma warning disable CS0109
    public new class MethodName : Control.MethodName
    {
        public new static readonly StringName _Ready = "_Ready";
        public new static readonly StringName _EnterTree = "_EnterTree";
        public new static readonly StringName _ExitTree = "_ExitTree";
        public new static readonly StringName _Notification = "_Notification";
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal new static System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        return
        [
            new(name: MethodName._Ready, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._EnterTree, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._ExitTree, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: null, defaultArguments: null),
            new(name: MethodName._Notification, returnVal: new(type: (Variant.Type)0, name: "", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false), flags: (MethodFlags)1, arguments: new() { new(type: (Variant.Type)2, name: "what", hint: 0, hintString: "", usage: (PropertyUsageFlags)6, exported: false) }, defaultArguments: null),
        ];
    }
#pragma warning restore CS0109

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName._Ready && args.Count == 0)
        {
            _Ready();
            ret = default;
            return true;
        }

        if (method == MethodName._EnterTree && args.Count == 0)
        {
            _EnterTree();
            ret = default;
            return true;
        }

        if (method == MethodName._ExitTree && args.Count == 0)
        {
            _ExitTree();
            ret = default;
            return true;
        }

        if (method == MethodName._Notification && args.Count == 1)
        {
            _Notification(VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }

        return base.InvokeGodotClassMethod(method, args, out ret);
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName._Ready || method == MethodName._EnterTree
            || method == MethodName._ExitTree || method == MethodName._Notification)
        {
            return true;
        }

        return base.HasGodotClassMethod(method);
    }
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
public abstract class View<TViewModel> : View
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
