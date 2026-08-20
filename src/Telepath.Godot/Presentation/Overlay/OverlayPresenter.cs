using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Instantiates overlay scenes into one slot. Covered layers stay in the tree;
/// only the removed layer is freed after <see cref="IViewTransition.PlayExitAsync"/>
/// (or immediately when the view does not implement it). Does not dispose
/// ViewModels. <see cref="Reset"/> and <see cref="Clear"/> skip animation.
/// </summary>
public sealed class OverlayPresenter
{
    private readonly Control _slot;
    private readonly ViewRegistry _registry;
    private readonly bool _blocksPassThrough;
    private readonly PresentedViews? _presented;
    private readonly List<Control> _views = [];
    private readonly HashSet<Control> _exiting = [];
    private readonly ViewTransitionSession _transitions = new();
    private bool _skipEnter;

    public OverlayPresenter(
        Control slot,
        ViewRegistry registry,
        bool blocksPassThrough = true,
        PresentedViews? presented = null)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        _slot = slot;
        _registry = registry;
        _blocksPassThrough = blocksPassThrough;
        _presented = presented;
        UpdateMouseFilter();
    }

    public OverlayTarget Target => new(Reset, Insert, RemoveAt, Clear);

    public void Reset(IReadOnlyList<IViewModel> items)
    {
        Clear();
        _skipEnter = true;
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                Insert(i, items[i]);
            }
        }
        finally
        {
            _skipEnter = false;
        }
    }

    public void Insert(int index, IViewModel viewModel)
    {
        var view = _registry.Create(viewModel.GetType());
        try
        {
            ViewInjection.Inject(view, viewModel);
        }
        catch
        {
            view.QueueFree();
            throw;
        }

        ApplyLayout(view);
        _views.Insert(index, view);
        _presented?.Set(viewModel, view);
        _slot.AddChild(view);
        PlaceLive(view, index);
        UpdateMouseFilter();
        if (!_skipEnter)
        {
            _transitions.PlayEnter(view);
        }
    }

    public void RemoveAt(int index)
    {
        var view = _views[index];
        _views.RemoveAt(index);
        BeginExit(view);
        UpdateMouseFilter();
    }

    public void Clear()
    {
        AbortAll();
        UpdateMouseFilter();
    }

    private void PlaceLive(Control view, int liveIndex)
    {
        if (liveIndex >= _views.Count - 1)
        {
            return;
        }

        var desired = _views[liveIndex + 1].GetIndex();
        if (view.GetIndex() != desired)
        {
            _slot.MoveChild(view, desired);
        }
    }

    private void BeginExit(Control view)
    {
        _presented?.MarkExiting(view);
        ViewInjection.Clear(view);
        ViewInjection.IgnoreMouse(view);
        _exiting.Add(view);
        _transitions.PlayExit(view, FinishExit);
    }

    private void FinishExit(Control view)
    {
        _exiting.Remove(view);
        _presented?.Remove(view);
        ViewInjection.Remove(view);
    }

    private void AbortAll()
    {
        _transitions.CancelAll();
        for (var i = _views.Count - 1; i >= 0; i--)
        {
            _presented?.Remove(_views[i]);
            ViewInjection.Remove(_views[i]);
        }

        _views.Clear();
        foreach (var view in _exiting.ToArray())
        {
            _exiting.Remove(view);
            _presented?.Remove(view);
            ViewInjection.Remove(view);
        }
    }

    private static void ApplyLayout(Control view)
    {
        view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        view.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        view.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    private void UpdateMouseFilter()
    {
        if (!_blocksPassThrough)
        {
            _slot.MouseFilter = Control.MouseFilterEnum.Ignore;
            return;
        }

        _slot.MouseFilter = _views.Count == 0
            ? Control.MouseFilterEnum.Ignore
            : Control.MouseFilterEnum.Stop;
    }
}
