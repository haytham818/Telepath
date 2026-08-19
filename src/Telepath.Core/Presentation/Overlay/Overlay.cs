using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Multi-layer overlay stack. Covered views stay in the tree with bindings.
/// <see cref="CoverMode.Pause"/> deactivates the covered item;
/// <see cref="CoverMode.Continue"/> leaves it running. The optional
/// <c>covered</c> callback is the screen underneath the first overlay.
/// </summary>
public class Overlay : ViewModel, IOverlay
{
    private readonly Func<IViewModel?>? _covered;
    private readonly List<CoverMode> _covers = [];
    private readonly bool _ownsActivation;

    public Overlay(Func<IViewModel?>? covered = null)
        : this(covered, ownsActivation: true)
    {
    }

    internal Overlay(Func<IViewModel?>? covered, bool ownsActivation)
    {
        _covered = covered;
        _ownsActivation = ownsActivation;
        Layers = new ObservableList<IViewModel>();
        HasOverlay = Track(new BindableReactiveProperty<bool>(false));
    }

    /// <summary>
    /// Cover mode of the overlay at <paramref name="index"/> in <see cref="Layers"/>.
    /// </summary>
    public CoverMode CoverAt(int index)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if ((uint)index >= (uint)_covers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _covers[index];
    }

    /// <inheritdoc />
    public ObservableList<IViewModel> Layers { get; }

    /// <inheritdoc />
    public BindableReactiveProperty<bool> HasOverlay { get; }

    /// <summary>
    /// Creates overlay ViewModels for <see cref="Push{T}"/>. The activator does
    /// not own the instance; this overlay disposes it when it leaves the stack.
    /// </summary>
    public IViewModelActivator? ViewModelActivator { get; set; }

    /// <inheritdoc />
    public void Push(IViewModel viewModel, CoverMode cover = CoverMode.Pause)
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

        if (_ownsActivation && cover == CoverMode.Pause)
        {
            if (Layers.Count > 0)
            {
                Deactivate(Layers[^1]);
            }
            else if (covered is not null)
            {
                Deactivate(covered);
            }
        }

        _covers.Add(cover);
        Layers.Add(viewModel);
        UpdateHasOverlay();
        if (_ownsActivation)
        {
            Activate(viewModel);
        }
    }

    /// <inheritdoc />
    public void Push<T>(CoverMode cover = CoverMode.Pause, params object[] arguments)
        where T : class, IViewModel
        => Push(ViewModelActivation.Create<T>(ViewModelActivator, arguments), cover);

    /// <inheritdoc />
    public bool Back()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (Layers.Count == 0)
        {
            return false;
        }

        var leaving = Layers[^1];
        var cover = _covers[^1];
        if (_ownsActivation)
        {
            Deactivate(leaving);
        }

        _covers.RemoveAt(_covers.Count - 1);
        Layers.RemoveAt(Layers.Count - 1);
        UpdateHasOverlay();
        leaving.Dispose();

        if (!_ownsActivation || cover != CoverMode.Pause)
        {
            return true;
        }

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

        _covers.RemoveAt(index);
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

        var pausedCovered = _covers[0] == CoverMode.Pause;
        if (_ownsActivation)
        {
            Deactivate(Layers[^1]);
        }

        var closing = new IViewModel[Layers.Count];
        for (var i = 0; i < Layers.Count; i++)
        {
            closing[i] = Layers[i];
        }

        _covers.Clear();
        Layers.Clear();
        UpdateHasOverlay();
        for (var i = closing.Length - 1; i >= 0; i--)
        {
            closing[i].Dispose();
        }

        if (_ownsActivation && resumeCovered && pausedCovered)
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
