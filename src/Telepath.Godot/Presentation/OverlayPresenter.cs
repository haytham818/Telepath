using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Instantiates overlay scenes into one slot. Covered layers stay in the tree;
/// only the removed layer is freed. Does not dispose ViewModels.
/// </summary>
public sealed class OverlayPresenter
{
    private readonly Control _slot;
    private readonly ViewRegistry _registry;
    private readonly List<Control> _views = [];

    public OverlayPresenter(Control slot, ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        _slot = slot;
        _registry = registry;
        UpdateMouseFilter();
    }

    public OverlayTarget Target => new(Reset, Insert, RemoveAt, Clear);

    public void Reset(IReadOnlyList<IViewModel> items)
    {
        Clear();
        for (var i = 0; i < items.Count; i++)
        {
            Insert(i, items[i]);
        }
    }

    public void Insert(int index, IViewModel viewModel)
    {
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

        ApplyLayout(view);
        _views.Insert(index, view);
        _slot.AddChild(view);
        if (index < _slot.GetChildCount() - 1)
        {
            _slot.MoveChild(view, index);
        }

        UpdateMouseFilter();
    }

    public void RemoveAt(int index)
    {
        var view = _views[index];
        _views.RemoveAt(index);
        ViewInjection.Remove(view);
        UpdateMouseFilter();
    }

    public void Clear()
    {
        for (var i = _views.Count - 1; i >= 0; i--)
        {
            ViewInjection.Remove(_views[i]);
        }

        _views.Clear();
        UpdateMouseFilter();
    }

    private static void ApplyLayout(Control view)
    {
        view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        view.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        view.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    private void UpdateMouseFilter()
    {
        _slot.MouseFilter = _views.Count == 0
            ? Control.MouseFilterEnum.Ignore
            : Control.MouseFilterEnum.Stop;
    }
}
