using R3;

namespace Telepath.Core;

/// <summary>
/// Single-slot navigation stack. <see cref="Navigate"/> pushes the current page,
/// <see cref="Back"/> pops it. Pages that leave the stack are disposed; first
/// period does not cache. Optional <see cref="IActivatable"/> hooks run around
/// the <see cref="ActiveItem"/> change and are independent of view bind/unbind.
/// </summary>
public class Conductor : ViewModel, IConductor
{
    private readonly List<IViewModel> _backStack = [];

    public Conductor()
    {
        ActiveItem = Track(new BindableReactiveProperty<IViewModel?>());
        CanGoBack = Track(new BindableReactiveProperty<bool>(false));
        BackCommand = Command(() => Back(), CanGoBack);
    }

    /// <inheritdoc />
    public BindableReactiveProperty<IViewModel?> ActiveItem { get; }

    /// <summary>
    /// <see langword="true"/> when <see cref="Back"/> would restore a previous page.
    /// </summary>
    public BindableReactiveProperty<bool> CanGoBack { get; }

    /// <summary>
    /// Executes <see cref="Back"/> when <see cref="CanGoBack"/> is true.
    /// </summary>
    public ReactiveCommand BackCommand { get; }

    /// <inheritdoc />
    public void Navigate(IViewModel viewModel)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);
        if (ReferenceEquals(viewModel, this))
        {
            throw new ArgumentException("A conductor cannot navigate to itself.", nameof(viewModel));
        }

        if (ReferenceEquals(ActiveItem.Value, viewModel))
        {
            return;
        }

        if (_backStack.Contains(viewModel))
        {
            throw new InvalidOperationException(
                "ViewModel is already on the back stack. First-period navigation does not cache or bring-to-front.");
        }

        var current = ActiveItem.Value;
        if (current is not null)
        {
            Deactivate(current);
            _backStack.Add(current);
        }

        ActiveItem.Value = viewModel;
        UpdateCanGoBack();
        Activate(viewModel);
    }

    /// <inheritdoc />
    public bool Back()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_backStack.Count == 0)
        {
            return false;
        }

        var leaving = ActiveItem.Value;
        var previous = _backStack[^1];
        _backStack.RemoveAt(_backStack.Count - 1);

        if (leaving is not null)
        {
            Deactivate(leaving);
        }

        ActiveItem.Value = previous;
        UpdateCanGoBack();
        leaving?.Dispose();
        Activate(previous);
        return true;
    }

    /// <inheritdoc />
    public void CloseSelf()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (ActiveItem.Value is { } current)
        {
            Close(current);
        }
    }

    /// <inheritdoc />
    public void Close(IViewModel viewModel)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);

        if (ReferenceEquals(ActiveItem.Value, viewModel))
        {
            if (Back())
            {
                return;
            }

            Deactivate(viewModel);
            ActiveItem.Value = null;
            UpdateCanGoBack();
            viewModel.Dispose();
            return;
        }

        var index = _backStack.IndexOf(viewModel);
        if (index < 0)
        {
            throw new InvalidOperationException(
                "ViewModel is not presented by this conductor.");
        }

        _backStack.RemoveAt(index);
        UpdateCanGoBack();
        viewModel.Dispose();
    }

    /// <inheritdoc />
    protected override void OnDispose()
    {
        var active = ActiveItem.Value;
        if (active is not null)
        {
            Deactivate(active);
            ActiveItem.Value = null;
            active.Dispose();
        }

        for (var i = _backStack.Count - 1; i >= 0; i--)
        {
            _backStack[i].Dispose();
        }

        _backStack.Clear();
        UpdateCanGoBack();
    }

    private void UpdateCanGoBack()
    {
        if (CanGoBack.IsDisposed)
        {
            return;
        }

        var canGoBack = _backStack.Count > 0;
        if (CanGoBack.Value != canGoBack)
        {
            CanGoBack.Value = canGoBack;
        }
    }

    private static void Activate(IViewModel viewModel)
    {
        if (viewModel is IActivatable activatable)
        {
            activatable.Activate();
        }
    }

    private static void Deactivate(IViewModel viewModel)
    {
        if (viewModel is IActivatable activatable)
        {
            activatable.Deactivate();
        }
    }
}
