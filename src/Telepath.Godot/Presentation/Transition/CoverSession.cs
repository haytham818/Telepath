using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Diffs the overlay host's covered set against live presented views and plays
/// <see cref="IViewCoverTransition"/> in parallel with overlay enter / exit.
/// </summary>
internal sealed class CoverSession
{
    private readonly PresentedViews _presented;
    private readonly HashSet<Control> _covered = [];
    private readonly ViewTransitionSession _runs = new();

    public CoverSession(PresentedViews presented)
    {
        _presented = presented;
    }

    public void Sync(IReadOnlyList<IViewModel> covered, IReadOnlyList<IViewModel> live)
    {
        var liveSet = new HashSet<IViewModel>(live);
        var desired = new HashSet<Control>();
        foreach (var viewModel in covered)
        {
            if (!liveSet.Contains(viewModel)
                || !_presented.TryGet(viewModel, out var view)
                || _presented.IsExiting(view)
                || !GodotObject.IsInstanceValid(view))
            {
                continue;
            }

            desired.Add(view);
        }

        foreach (var view in _covered.ToArray())
        {
            if (desired.Contains(view))
            {
                continue;
            }

            _covered.Remove(view);
            if (!GodotObject.IsInstanceValid(view)
                || _presented.IsExiting(view)
                || !_presented.TryGetViewModel(view, out var viewModel)
                || !liveSet.Contains(viewModel))
            {
                _runs.Cancel(view);
                continue;
            }

            _runs.PlayUncover(view);
        }

        foreach (var view in desired)
        {
            if (!_covered.Add(view))
            {
                continue;
            }

            _runs.PlayCover(view);
        }
    }

    public void Cancel(Control view)
    {
        _covered.Remove(view);
        _runs.Cancel(view);
    }

    public void CancelAll()
    {
        _covered.Clear();
        _runs.CancelAll();
    }
}
