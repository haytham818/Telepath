using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Owns the ViewModel and binding lifecycle for one host-assembly Godot view.
/// This type deliberately does not inherit <see cref="GodotObject"/>.
/// </summary>
/// <typeparam name="TViewModel">The strongly typed ViewModel used by the view.</typeparam>
public sealed class ViewLifecycle<TViewModel>
    where TViewModel : class, IViewModel
{
    private readonly Control _owner;
    private readonly Action _onReady;
    private readonly Func<TViewModel> _createViewModel;
    private readonly Action<TViewModel, BindingSet> _onBind;
    private readonly Action<TViewModel> _onUnbind;
    private readonly Action _onEnterTree;
    private readonly Action _onExitTree;
    private readonly Action _onPredelete;
    private BindingSet? _bindings;
    private TViewModel? _viewModel;
    private bool _ownsViewModel;

    public ViewLifecycle(
        Control owner,
        Action onReady,
        Func<TViewModel> createViewModel,
        Action<TViewModel, BindingSet> onBind,
        Action<TViewModel> onUnbind,
        Action onEnterTree,
        Action onExitTree,
        Action onPredelete)
    {
        _owner = owner;
        _onReady = onReady;
        _createViewModel = createViewModel;
        _onBind = onBind;
        _onUnbind = onUnbind;
        _onEnterTree = onEnterTree;
        _onExitTree = onExitTree;
        _onPredelete = onPredelete;
    }

    /// <summary>
    /// Gets or injects the ViewModel. A null value is replaced during the ready
    /// notification. Setting a new instance after ready rebinds. Injected
    /// instances are not disposed when the owner is freed; a self-created
    /// instance is disposed when replaced or when the owner is freed.
    /// </summary>
    public TViewModel? ViewModel
    {
        get => _viewModel;
        set => SetViewModel(value, owns: false);
    }

    /// <summary>
    /// Handles the Godot notifications forwarded by the generated host-side bridge.
    /// </summary>
    public void HandleNotification(int what)
    {
        switch (what)
        {
            case (int)Node.NotificationReady:
                _onReady();
                if (_viewModel is null)
                {
                    SetViewModel(_createViewModel(), owns: true);
                }
                else
                {
                    AttachBindings();
                }

                break;

            case (int)Node.NotificationEnterTree:
                if (_viewModel is not null && _bindings is null && _owner.IsNodeReady())
                {
                    AttachBindings();
                }

                _onEnterTree();
                break;

            case (int)Node.NotificationExitTree:
                DetachBindings();
                _onExitTree();
                break;

            case (int)GodotObject.NotificationPredelete:
                try
                {
                    _onPredelete();
                }
                finally
                {
                    Release();
                }

                break;
        }
    }

    /// <summary>
    /// Drops bindings and, when this lifecycle owns the ViewModel, disposes it.
    /// Safe to call more than once.
    /// </summary>
    public void Release()
    {
        DetachBindings();
        if (_ownsViewModel)
        {
            _viewModel?.Dispose();
        }

        _viewModel = null;
        _ownsViewModel = false;
    }

    private void SetViewModel(TViewModel? value, bool owns)
    {
        if (ReferenceEquals(_viewModel, value))
        {
            if (value is not null)
            {
                _ownsViewModel = owns;
            }

            return;
        }

        DetachBindings();
        if (_ownsViewModel)
        {
            _viewModel?.Dispose();
        }

        _viewModel = value;
        _ownsViewModel = owns && value is not null;

        if (value is not null && _owner.IsNodeReady() && _owner.IsInsideTree())
        {
            AttachBindings();
        }
    }

    private void AttachBindings()
    {
        if (_viewModel is null || _viewModel.IsDisposed || _bindings is not null)
        {
            return;
        }

        var bindings = new BindingSet();
        try
        {
            _onBind(_viewModel, bindings);
            _bindings = bindings;
        }
        catch
        {
            bindings.Dispose();
            throw;
        }

        if (_viewModel is ViewModel viewModel)
        {
            viewModel.NotifyBound();
        }
    }

    private void DetachBindings()
    {
        if (_bindings is null)
        {
            return;
        }

        var viewModel = _viewModel;
        var bindings = _bindings;
        _bindings = null;

        try
        {
            if (viewModel is not null)
            {
                _onUnbind(viewModel);
            }
        }
        finally
        {
            try
            {
                if (viewModel is ViewModel coreViewModel)
                {
                    coreViewModel.NotifyUnbound();
                }
            }
            finally
            {
                bindings.Dispose();
            }
        }
    }
}
