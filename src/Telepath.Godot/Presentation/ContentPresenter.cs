using System.Reflection;
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
            Inject(view, viewModel);
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

        Remove(_current);
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

    private static void Inject(Control view, IViewModel viewModel)
    {
        var property = view.GetType().GetProperty(
            "ViewModel",
            BindingFlags.Instance | BindingFlags.Public);
        if (property is null || !property.CanWrite)
        {
            throw new InvalidOperationException(
                $"Instantiated view '{view.GetType().Name}' does not implement ITelepathView<>.");
        }

        if (!property.PropertyType.IsAssignableFrom(viewModel.GetType()))
        {
            throw new InvalidOperationException(
                $"Cannot inject '{viewModel.GetType().Name}' into '{view.GetType().Name}.ViewModel' ({property.PropertyType.Name}).");
        }

        property.SetValue(view, viewModel);
    }

    private static void Remove(Control view)
    {
        var parent = view.GetParent();
        parent?.RemoveChild(view);
        view.QueueFree();
    }
}
