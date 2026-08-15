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
    private BindingSet? _bindings;

    public ViewLifecycle(
        Control owner,
        Action onReady,
        Func<TViewModel> createViewModel,
        Action<TViewModel, BindingSet> onBind)
    {
        _owner = owner;
        _onReady = onReady;
        _createViewModel = createViewModel;
        _onBind = onBind;
    }

    /// <summary>
    /// Gets or injects the ViewModel. A null value is replaced during the ready notification.
    /// </summary>
    public TViewModel? ViewModel { get; set; }

    /// <summary>
    /// Handles the Godot notifications forwarded by the generated host-side bridge.
    /// </summary>
    public void HandleNotification(int what)
    {
        switch (what)
        {
            case (int)Node.NotificationReady:
                _onReady();
                ViewModel ??= _createViewModel();
                AttachBindings();
                break;

            case (int)Node.NotificationEnterTree:
                if (ViewModel is not null && _bindings is null && _owner.IsNodeReady())
                {
                    AttachBindings();
                }

                break;

            case (int)Node.NotificationExitTree:
                DetachBindings();
                break;

            case (int)GodotObject.NotificationPredelete:
                DetachBindings();
                ViewModel?.Dispose();
                ViewModel = null;
                break;
        }
    }

    private void AttachBindings()
    {
        if (ViewModel is null || ViewModel.IsDisposed || _bindings is not null)
        {
            return;
        }

        var bindings = new BindingSet();
        try
        {
            _onBind(ViewModel, bindings);
            _bindings = bindings;
        }
        catch
        {
            bindings.Dispose();
            throw;
        }
    }

    private void DetachBindings()
    {
        _bindings?.Dispose();
        _bindings = null;
    }
}
