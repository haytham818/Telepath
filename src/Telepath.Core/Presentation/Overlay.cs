using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Multi-layer overlay stack. Covered views stay in the tree with bindings;
/// only the top layer is <see cref="IActivatable.Activate"/>d. The optional
/// <c>covered</c> callback is the screen underneath the first overlay.
/// </summary>
public class Overlay : ViewModel, IOverlay
{
    private readonly Func<IViewModel?>? _covered;

    public Overlay(Func<IViewModel?>? covered = null)
    {
        _covered = covered;
        Layers = new ObservableList<IViewModel>();
        HasOverlay = Track(new BindableReactiveProperty<bool>(false));
    }

    /// <inheritdoc />
    public ObservableList<IViewModel> Layers { get; }

    /// <inheritdoc />
    public BindableReactiveProperty<bool> HasOverlay { get; }

    /// <inheritdoc />
    public void Push(IViewModel viewModel)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);
        if (ReferenceEquals(viewModel, this))
        {
            throw new ArgumentException("An overlay cannot push itself.", nameof(viewModel));
        }

        if (Layers.Count > 0 && ReferenceEquals(Layers[^1], viewModel))
        {
            return;
        }

        if (Layers.Contains(viewModel))
        {
            throw new InvalidOperationException(
                "ViewModel is already on the overlay stack.");
        }

        var covered = Covered();
        if (covered is not null && ReferenceEquals(covered, viewModel))
        {
            throw new ArgumentException(
                "Cannot push the covered screen as an overlay.", nameof(viewModel));
        }

        if (Layers.Count > 0)
        {
            Deactivate(Layers[^1]);
        }
        else if (covered is not null)
        {
            Deactivate(covered);
        }

        Layers.Add(viewModel);
        UpdateHasOverlay();
        Activate(viewModel);
    }

    /// <inheritdoc />
    public bool Back()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Layers.Count == 0)
        {
            return false;
        }

        var leaving = Layers[^1];
        Deactivate(leaving);
        Layers.RemoveAt(Layers.Count - 1);
        UpdateHasOverlay();
        leaving.Dispose();

        if (Layers.Count > 0)
        {
            Activate(Layers[^1]);
        }
        else
        {
            ResumeCovered();
        }

        return true;
    }

    /// <inheritdoc />
    public void Close(IViewModel viewModel)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);

        var index = Layers.IndexOf(viewModel);
        if (index < 0)
        {
            throw new InvalidOperationException(
                "ViewModel is not presented by this overlay.");
        }

        if (index == Layers.Count - 1)
        {
            Back();
            return;
        }

        Layers.RemoveAt(index);
        UpdateHasOverlay();
        viewModel.Dispose();
    }

    /// <inheritdoc />
    public void CloseSelf()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Layers.Count > 0)
        {
            Close(Layers[^1]);
        }
    }

    /// <inheritdoc />
    public void Clear(bool resumeCovered = true)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ClearCore(resumeCovered);
    }

    /// <inheritdoc />
    protected override void OnDispose()
    {
        ClearCore(resumeCovered: false);
    }

    private void ClearCore(bool resumeCovered)
    {
        if (Layers.Count == 0)
        {
            return;
        }

        Deactivate(Layers[^1]);
        var closing = new IViewModel[Layers.Count];
        for (var i = 0; i < Layers.Count; i++)
        {
            closing[i] = Layers[i];
        }

        Layers.Clear();
        UpdateHasOverlay();
        for (var i = closing.Length - 1; i >= 0; i--)
        {
            closing[i].Dispose();
        }

        if (resumeCovered)
        {
            ResumeCovered();
        }
    }

    private IViewModel? Covered()
    {
        var covered = _covered?.Invoke();
        return covered is { IsDisposed: false } ? covered : null;
    }

    private void ResumeCovered()
    {
        var covered = Covered();
        if (covered is not null)
        {
            Activate(covered);
        }
    }

    private void UpdateHasOverlay()
    {
        if (HasOverlay.IsDisposed)
        {
            return;
        }

        var hasOverlay = Layers.Count > 0;
        if (HasOverlay.Value != hasOverlay)
        {
            HasOverlay.Value = hasOverlay;
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
