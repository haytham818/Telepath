using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Shared ViewModel → live Control map for content and overlay presenters.
/// Pass the same instance to <c>Content</c> and <c>BindOverlayHost</c> so a
/// covered screen or lower overlay can play <see cref="IViewCoverTransition"/>
/// and so <see cref="IOverlayHost.InputBlocked"/> can isolate GUI focus.
/// </summary>
public sealed class PresentedViews
{
    private readonly Dictionary<IViewModel, Control> _views = [];
    private readonly Dictionary<Control, IViewModel> _viewModels = [];
    private readonly HashSet<Control> _exiting = [];

    public PresentedViews()
    {
        Covers = new CoverSession(this);
        Focus = new FocusSession(this);
    }

    internal CoverSession Covers { get; }

    internal FocusSession Focus { get; }

    internal void Set(IViewModel viewModel, Control view)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(view);
        if (_viewModels.Remove(view, out var previous))
        {
            _views.Remove(previous);
        }

        if (_views.Remove(viewModel, out var displaced) && !ReferenceEquals(displaced, view))
        {
            _viewModels.Remove(displaced);
            _exiting.Remove(displaced);
        }

        _views[viewModel] = view;
        _viewModels[view] = viewModel;
        _exiting.Remove(view);
        Focus.OnPresented(viewModel, view);
    }

    internal void MarkExiting(Control view)
    {
        _exiting.Add(view);
        Covers.Cancel(view);
        Focus.Cancel(view);
    }

    internal void Remove(Control view)
    {
        Covers.Cancel(view);
        Focus.Cancel(view);
        _exiting.Remove(view);
        if (_viewModels.Remove(view, out var viewModel))
        {
            if (_views.TryGetValue(viewModel, out var mapped) && ReferenceEquals(mapped, view))
            {
                _views.Remove(viewModel);
            }
        }
    }

    internal bool IsExiting(Control view) => _exiting.Contains(view);

    internal bool TryGet(IViewModel viewModel, out Control view)
        => _views.TryGetValue(viewModel, out view!);

    internal bool TryGetViewModel(Control view, out IViewModel viewModel)
        => _viewModels.TryGetValue(view, out viewModel!);
}
