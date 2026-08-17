using Godot;
using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Creates one overlay slot per registered band under a root <see cref="Control"/>.
/// Sibling order follows <see cref="IOverlayHost.Bands"/>. Does not dispose ViewModels.
/// </summary>
public sealed class OverlayHostPresenter
{
    private readonly Control _root;
    private readonly ViewRegistry _registry;
    private readonly PresentedViews _presented;
    private readonly List<Control> _slots = [];

    public OverlayHostPresenter(Control root, ViewRegistry registry, PresentedViews? presented = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(registry);
        _root = root;
        _registry = registry;
        _presented = presented ?? new PresentedViews();
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    public void Bind(IOverlayHost host, BindingSet bindings)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(bindings);

        bindings.Add(Disposable.Create(Detach));
        foreach (var layer in host.Bands)
        {
            var slot = CreateSlot(layer);
            var presenter = new OverlayPresenter(slot, _registry, layer.BlocksPassThrough, _presented);
            bindings.BindOverlay(host.Band(layer).Layers, presenter.Target);
        }

        bindings.Add(host.Covered.Subscribe(_ => Sync(host)));
        Sync(host);
    }

    private void Sync(IOverlayHost host)
    {
        var live = new List<IViewModel>();
        if (host.CurrentScreen is { IsDisposed: false } screen)
        {
            live.Add(screen);
        }

        foreach (var layer in host.Bands)
        {
            live.AddRange(host.Band(layer).Layers);
        }

        _presented.Covers.Sync(host.Covered.Value, live);
    }

    private Control CreateSlot(OverlayLayer layer)
    {
        var slot = new Control
        {
            Name = layer.Name,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        slot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        slot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        slot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _slots.Add(slot);
        _root.AddChild(slot);
        return slot;
    }

    private void Detach()
    {
        _presented.Covers.CancelAll();
        for (var i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            var parent = slot.GetParent();
            parent?.RemoveChild(slot);
            slot.QueueFree();
        }

        _slots.Clear();
    }
}
