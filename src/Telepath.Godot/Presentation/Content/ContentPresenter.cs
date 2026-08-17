using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Instantiates a registered scene into one slot. Injects the ViewModel before
/// the child enters the tree. Frees the view node on replace after
/// <see cref="IViewTransition.PlayExitAsync"/> (or immediately when the view
/// does not implement it). Does not dispose the ViewModel (the conductor owns
/// it). <see cref="Clear"/> skips animation.
/// </summary>
public sealed class ContentPresenter
{
    private readonly Control _slot;
    private readonly ViewRegistry _registry;
    private readonly PresentedViews? _presented;
    private readonly ViewTransitionSession _transitions = new();
    private readonly HashSet<Control> _exiting = [];
    private Control? _current;

    public ContentPresenter(Control slot, ViewRegistry registry, PresentedViews? presented = null)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        _slot = slot;
        _registry = registry;
        _presented = presented;
    }

    public void Present(IViewModel? viewModel)
    {
        if (viewModel is null)
        {
            Clear();
            return;
        }

        var scene = _registry.Resolve(viewModel.GetType());
        var view = scene.Instantiate<Control>();
        try
        {
            ViewInjection.Inject(view, viewModel);
        }
        catch
        {
            view.QueueFree();
            throw;
        }

        DismissCurrent();
        Attach(view, viewModel);
        _transitions.PlayEnter(view);
    }

    public void Clear()
    {
        AbortAll();
    }

    private void DismissCurrent()
    {
        AbortExiting();
        if (_current is null)
        {
            return;
        }

        var leaving = _current;
        _current = null;
        BeginExit(leaving);
    }

    private void Attach(Control view, IViewModel viewModel)
    {
        ApplyLayout(view);
        _slot.AddChild(view);
        _current = view;
        _presented?.Set(viewModel, view);
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

    private void AbortExiting()
    {
        foreach (var view in _exiting.ToArray())
        {
            ForceFree(view);
        }
    }

    private void AbortAll()
    {
        _transitions.CancelAll();
        if (_current is not null)
        {
            _presented?.Remove(_current);
            ViewInjection.Remove(_current);
            _current = null;
        }

        foreach (var view in _exiting.ToArray())
        {
            _exiting.Remove(view);
            _presented?.Remove(view);
            ViewInjection.Remove(view);
        }
    }

    private void ForceFree(Control view)
    {
        _transitions.Cancel(view);
        _exiting.Remove(view);
        _presented?.Remove(view);
        ViewInjection.Remove(view);
    }

    private void ApplyLayout(Control view)
    {
        view.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        view.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        if (_slot is not Container)
        {
            view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
    }
}
