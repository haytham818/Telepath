using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Instantiates a registered scene into one slot. Injects the ViewModel before
/// the child enters the tree. Frees the view node on replace; does not dispose
/// the ViewModel (the conductor owns it).
/// </summary>
public sealed class ContentPresenter
{
    private readonly Control _slot;
    private readonly ViewRegistry _registry;
    private Control? _current;

    public ContentPresenter(Control slot, ViewRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(registry);
        _slot = slot;
        _registry = registry;
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

        Clear();
        Attach(view);
    }

    public void Clear()
    {
        if (_current is null)
        {
            return;
        }

        ViewInjection.Remove(_current);
        _current = null;
    }

    private void Attach(Control view)
    {
        ApplyLayout(view);
        _slot.AddChild(view);
        _current = view;
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
