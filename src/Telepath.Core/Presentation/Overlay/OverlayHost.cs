using ObservableCollections;
using R3;

namespace Telepath.Core;

/// <summary>
/// Independent overlay stacks keyed by <see cref="OverlayLayer"/>.
/// Activation is reconciled across bands so a Pause modal covers lower bands
/// without a lower-band Push activating underneath it. <see cref="Covered"/>
/// is visual z-order; <see cref="InputBlocked"/> is keyboard/gamepad isolation.
/// </summary>
public sealed class OverlayHost : ViewModel, IOverlayHost
{
    private readonly Func<IViewModel?>? _covered;
    private readonly List<LayerBand> _bands = [];
    private readonly Dictionary<string, LayerBand> _byName = new(StringComparer.Ordinal);
    private readonly HashSet<IViewModel> _active = [];
    private readonly List<OverlayLayer> _layerList = [];
    private int _suspendReconcile;
    private bool _locked;
    private bool _suppressScreenActivate;
    private IViewModelActivator? _viewModelActivator;
    private IViewModel? _inheritedScreen;
    private IViewModel? _holdCoveredScreen;

    public OverlayHost(Func<IViewModel?>? covered = null)
    {
        _covered = covered;
        HasOverlay = Track(new BindableReactiveProperty<bool>(false));
        HasBackableOverlay = Track(new BindableReactiveProperty<bool>(false));
        Covered = Track(new BindableReactiveProperty<IReadOnlyList<IViewModel>>([]));
        InputBlocked = Track(new BindableReactiveProperty<IReadOnlyList<IViewModel>>([]));
        Register(OverlayLayer.Popup);
        Register(OverlayLayer.Modal);
        Register(OverlayLayer.Toast);
    }

    /// <inheritdoc />
    public ObservableList<IViewModel> Layers => Popup.Layers;

    /// <inheritdoc />
    public BindableReactiveProperty<bool> HasOverlay { get; }

    /// <inheritdoc />
    public BindableReactiveProperty<bool> HasBackableOverlay { get; }

    /// <inheritdoc />
    public IViewModel? CurrentScreen => GetScreen();

    /// <inheritdoc />
    public BindableReactiveProperty<IReadOnlyList<IViewModel>> Covered { get; }

    /// <inheritdoc />
    public BindableReactiveProperty<IReadOnlyList<IViewModel>> InputBlocked { get; }

    /// <inheritdoc />
    public IReadOnlyList<OverlayLayer> Bands => _layerList;

    /// <summary>
    /// Creates overlay ViewModels for <see cref="Push{T}"/>. The activator does
    /// not own the instance; this host disposes it when it leaves the stack.
    /// Assigned onto each band so <see cref="Band"/> can <c>Push&lt;T&gt;</c>.
    /// </summary>
    public IViewModelActivator? ViewModelActivator
    {
        get => _viewModelActivator;
        set
        {
            _viewModelActivator = value;
            foreach (var band in _bands)
            {
                band.Stack.ViewModelActivator = value;
            }
        }
    }

    /// <inheritdoc />
    public void Register(OverlayLayer layer)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (_locked)
        {
            throw new InvalidOperationException(
                "Cannot register overlay layers after an overlay has been pushed.");
        }

        if (_byName.ContainsKey(layer.Name))
        {
            throw new InvalidOperationException(
                $"Overlay layer '{layer.Name}' is already registered.");
        }

        foreach (var existing in _bands)
        {
            if (existing.Layer.Order == layer.Order)
            {
                throw new InvalidOperationException(
                    $"Overlay layer order {layer.Order} is already used by '{existing.Layer.Name}'.");
            }
        }

        var captured = layer;
        var overlay = Track(new Overlay(() => ItemBelow(captured), ownsActivation: false));
        overlay.ViewModelActivator = _viewModelActivator;
        overlay.Layers.CollectionChanged += OnBandCollectionChanged;
        var band = new LayerBand(layer, overlay);
        var index = _bands.Count;
        for (var i = 0; i < _bands.Count; i++)
        {
            if (layer.Order < _bands[i].Layer.Order)
            {
                index = i;
                break;
            }
        }

        _bands.Insert(index, band);
        _layerList.Insert(index, layer);
        _byName[layer.Name] = band;
    }

    /// <inheritdoc />
    public IOverlay Band(OverlayLayer layer) => Resolve(layer).Stack;

    /// <inheritdoc />
    public void Push(IViewModel viewModel, CoverMode cover = CoverMode.Pause) =>
        Push(viewModel, OverlayLayer.Popup, cover);

    /// <inheritdoc />
    public void Push(IViewModel viewModel, OverlayLayer layer, CoverMode? cover = null)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);
        if (ReferenceEquals(viewModel, this))
        {
            throw new ArgumentException("An overlay cannot push itself.", nameof(viewModel));
        }

        var screen = GetScreen();
        if (screen is not null && ReferenceEquals(screen, viewModel))
        {
            throw new ArgumentException(
                "Cannot push the covered screen as an overlay.", nameof(viewModel));
        }

        var existing = FindBand(viewModel);
        if (existing is not null)
        {
            if (existing.Layer.Name == layer.Name &&
                existing.Stack.Layers.Count > 0 &&
                ReferenceEquals(existing.Stack.Layers[^1], viewModel))
            {
                return;
            }

            throw new InvalidOperationException(
                "ViewModel is already on an overlay stack.");
        }

        var band = Resolve(layer);
        band.Stack.Push(viewModel, cover ?? band.Layer.DefaultCover);
    }

    /// <inheritdoc />
    public void Push<T>(CoverMode cover = CoverMode.Pause, params object[] arguments)
        where T : class, IViewModel
        => Push(ViewModelActivation.Create<T>(ViewModelActivator, arguments), cover);

    /// <inheritdoc />
    public void Push<T>(OverlayLayer layer, CoverMode? cover = null, params object[] arguments)
        where T : class, IViewModel
        => Push(ViewModelActivation.Create<T>(ViewModelActivator, arguments), layer, cover);

    /// <inheritdoc />
    public bool Back()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        for (var i = _bands.Count - 1; i >= 0; i--)
        {
            var band = _bands[i];
            if (!band.Layer.HandlesBack || band.Stack.Layers.Count == 0)
            {
                continue;
            }

            return band.Stack.Back();
        }

        return false;
    }

    /// <inheritdoc />
    public void Close(IViewModel viewModel)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(viewModel);
        ObjectDisposedException.ThrowIf(viewModel.IsDisposed, viewModel);

        var band = FindBand(viewModel);
        if (band is null)
        {
            throw new InvalidOperationException(
                "ViewModel is not presented by this overlay.");
        }

        band.Stack.Close(viewModel);
    }

    /// <inheritdoc />
    public void CloseSelf()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        for (var i = _bands.Count - 1; i >= 0; i--)
        {
            if (_bands[i].Stack.Layers.Count > 0)
            {
                _bands[i].Stack.CloseSelf();
                return;
            }
        }
    }

    /// <inheritdoc />
    public void Clear(bool resumeCovered = true)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _holdCoveredScreen = resumeCovered ? null : GetScreen();
        _suppressScreenActivate = !resumeCovered;
        _suspendReconcile++;
        try
        {
            DeactivateOverlays();
            for (var i = _bands.Count - 1; i >= 0; i--)
            {
                _bands[i].Stack.Clear(resumeCovered: false);
            }
        }
        finally
        {
            _suspendReconcile--;
            Reconcile();
            _suppressScreenActivate = false;
        }
    }

    /// <inheritdoc />
    protected override void OnDispose()
    {
        _suspendReconcile++;
        try
        {
            foreach (var band in _bands)
            {
                band.Stack.Layers.CollectionChanged -= OnBandCollectionChanged;
            }

            DeactivateOverlays();
            foreach (var band in _bands)
            {
                band.Stack.Clear(resumeCovered: false);
            }
        }
        finally
        {
            _suspendReconcile--;
            _active.Clear();
            _inheritedScreen = null;
            _holdCoveredScreen = null;
        }
    }

    private Overlay Popup => _byName[OverlayLayer.Popup.Name].Stack;

    private LayerBand Resolve(OverlayLayer layer)
    {
        if (!_byName.TryGetValue(layer.Name, out var band))
        {
            throw new InvalidOperationException(
                $"Unknown overlay layer '{layer.Name}'.");
        }

        return band;
    }

    private LayerBand? FindBand(IViewModel viewModel)
    {
        foreach (var band in _bands)
        {
            if (band.Stack.Layers.Contains(viewModel))
            {
                return band;
            }
        }

        return null;
    }

    private IViewModel? ItemBelow(OverlayLayer layer)
    {
        for (var i = _bands.Count - 1; i >= 0; i--)
        {
            if (_bands[i].Layer.Order >= layer.Order)
            {
                continue;
            }

            var layers = _bands[i].Stack.Layers;
            if (layers.Count > 0)
            {
                return layers[^1];
            }
        }

        return GetScreen();
    }

    private void DeactivateOverlays()
    {
        for (var b = _bands.Count - 1; b >= 0; b--)
        {
            var layers = _bands[b].Stack.Layers;
            for (var i = layers.Count - 1; i >= 0; i--)
            {
                var item = layers[i];
                if (!_active.Remove(item))
                {
                    continue;
                }

                Deactivate(item);
            }
        }
    }

    private IViewModel? GetScreen()
    {
        var covered = _covered?.Invoke();
        return covered is { IsDisposed: false } ? covered : null;
    }

    private void OnBandCollectionChanged(
        in NotifyCollectionChangedEventArgs<IViewModel> _)
    {
        _locked = true;
        if (_suspendReconcile == 0)
        {
            Reconcile();
        }
        else
        {
            UpdateFlags();
        }
    }

    private void Reconcile()
    {
        var desired = new HashSet<IViewModel>();
        var screen = GetScreen();
        InheritScreen(screen);
        IViewModel? foreground = screen;
        if (foreground is not null)
        {
            desired.Add(foreground);
        }

        foreach (var band in _bands)
        {
            var overlay = band.Stack;
            for (var i = 0; i < overlay.Layers.Count; i++)
            {
                var item = overlay.Layers[i];
                if (overlay.CoverAt(i) == CoverMode.Pause && foreground is not null)
                {
                    desired.Remove(foreground);
                }

                desired.Add(item);
                foreground = item;
            }
        }

        if (_suppressScreenActivate && screen is not null)
        {
            if (_active.Contains(screen))
            {
                desired.Add(screen);
            }
            else
            {
                desired.Remove(screen);
            }
        }

        foreach (var item in _active)
        {
            if (item.IsDisposed || desired.Contains(item))
            {
                continue;
            }

            Deactivate(item);
        }

        _active.RemoveWhere(item => item.IsDisposed || !desired.Contains(item));

        foreach (var item in desired)
        {
            if (item.IsDisposed || _active.Contains(item))
            {
                continue;
            }

            Activate(item);
            _active.Add(item);
        }

        UpdateFlags();
    }

    private void InheritScreen(IViewModel? screen)
    {
        if (ReferenceEquals(_inheritedScreen, screen))
        {
            return;
        }

        if (_holdCoveredScreen is not null && !ReferenceEquals(_holdCoveredScreen, screen))
        {
            _holdCoveredScreen = null;
        }

        if (_inheritedScreen is not null)
        {
            _active.Remove(_inheritedScreen);
        }

        _inheritedScreen = screen;
        if (screen is not null)
        {
            _active.Add(screen);
        }
    }

    private void UpdateFlags()
    {
        if (HasOverlay.IsDisposed)
        {
            return;
        }

        var hasOverlay = false;
        var hasBackable = false;
        foreach (var band in _bands)
        {
            if (band.Stack.Layers.Count == 0)
            {
                continue;
            }

            hasOverlay = true;
            if (band.Layer.HandlesBack)
            {
                hasBackable = true;
            }
        }

        if (HasOverlay.Value != hasOverlay)
        {
            HasOverlay.Value = hasOverlay;
        }

        if (HasBackableOverlay.Value != hasBackable)
        {
            HasBackableOverlay.Value = hasBackable;
        }

        UpdateCovered();
        UpdateInputBlocked();
    }

    private void UpdateCovered()
    {
        if (Covered.IsDisposed)
        {
            return;
        }

        var stacked = new List<IViewModel>();
        var screen = GetScreen();
        if (screen is not null)
        {
            stacked.Add(screen);
        }

        foreach (var band in _bands)
        {
            stacked.AddRange(band.Stack.Layers);
        }

        var covered = new List<IViewModel>();
        for (var i = 0; i < stacked.Count - 1; i++)
        {
            covered.Add(stacked[i]);
        }

        if (_holdCoveredScreen is { IsDisposed: false } held
            && !covered.Contains(held))
        {
            covered.Add(held);
        }

        Covered.Value = covered;
    }

    private void UpdateInputBlocked()
    {
        if (InputBlocked.IsDisposed)
        {
            return;
        }

        var stacked = new List<(IViewModel Item, bool BlocksBelow)>();
        var screen = GetScreen();
        if (screen is not null)
        {
            stacked.Add((screen, false));
        }

        foreach (var band in _bands)
        {
            foreach (var item in band.Stack.Layers)
            {
                stacked.Add((item, band.Layer.BlocksPassThrough));
            }
        }

        var blocked = new List<IViewModel>();
        for (var i = 0; i < stacked.Count; i++)
        {
            var blockedByAbove = false;
            for (var j = i + 1; j < stacked.Count; j++)
            {
                if (!stacked[j].BlocksBelow)
                {
                    continue;
                }

                blockedByAbove = true;
                break;
            }

            if (blockedByAbove)
            {
                blocked.Add(stacked[i].Item);
            }
        }

        InputBlocked.Value = blocked;
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

    private sealed class LayerBand(OverlayLayer layer, Overlay stack)
    {
        public OverlayLayer Layer { get; } = layer;
        public Overlay Stack { get; } = stack;
    }
}
