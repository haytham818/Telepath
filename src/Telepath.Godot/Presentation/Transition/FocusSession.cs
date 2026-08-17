using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Diffs the overlay host's input-blocked set against live presented views,
/// disables GUI focus on blocked trees, and restores or takes focus on the
/// blocking overlay that is currently in front.
/// </summary>
internal sealed class FocusSession
{
    private readonly PresentedViews _presented;
    private readonly Dictionary<Control, Snapshot> _blocked = [];
    private IViewModel? _pendingForeground;
    private Control? _foreground;

    public FocusSession(PresentedViews presented)
    {
        _presented = presented;
    }

    public void Sync(
        IReadOnlyList<IViewModel> inputBlocked,
        IReadOnlyList<IViewModel> live,
        IViewModel? foreground)
    {
        var liveSet = new HashSet<IViewModel>(live);
        var desired = new HashSet<Control>();
        foreach (var viewModel in inputBlocked)
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

        var restored = new HashSet<Control>();
        foreach (var view in _blocked.Keys.ToArray())
        {
            if (desired.Contains(view))
            {
                continue;
            }

            if (!Unblock(view, liveSet))
            {
                continue;
            }

            restored.Add(view);
        }

        foreach (var view in desired)
        {
            if (_blocked.ContainsKey(view))
            {
                continue;
            }

            Block(view);
        }

        _pendingForeground = foreground;
        TryTakeForeground(foreground, restored);
    }

    public void OnPresented(IViewModel viewModel, Control view)
    {
        if (!ReferenceEquals(viewModel, _pendingForeground)
            || _blocked.ContainsKey(view)
            || _presented.IsExiting(view))
        {
            return;
        }

        TakeForeground(view);
    }

    public void Cancel(Control view)
    {
        _blocked.Remove(view);
        if (ReferenceEquals(_foreground, view))
        {
            _foreground = null;
        }

        ViewFocus.ReleaseIfOwned(view);
    }

    public void CancelAll()
    {
        foreach (var view in _blocked.Keys.ToArray())
        {
            ViewFocus.ReleaseIfOwned(view);
        }

        _blocked.Clear();
        _foreground = null;
        _pendingForeground = null;
    }

    private void TryTakeForeground(IViewModel? foreground, HashSet<Control> restored)
    {
        if (foreground is null)
        {
            _foreground = null;
            return;
        }

        if (!_presented.TryGet(foreground, out var view)
            || _presented.IsExiting(view)
            || !GodotObject.IsInstanceValid(view))
        {
            return;
        }

        if (restored.Contains(view))
        {
            _foreground = view;
            return;
        }

        TakeForeground(view);
    }

    private void TakeForeground(Control view)
    {
        if (ReferenceEquals(_foreground, view))
        {
            return;
        }

        _foreground = view;
        ViewFocus.Take(view);
    }

    private void Block(Control view)
    {
        var snapshot = new Snapshot();
        Capture(view, snapshot);
        var owner = view.GetViewport()?.GuiGetFocusOwner();
        if (owner is not null && ViewFocus.IsDescendantOrSelf(view, owner))
        {
            snapshot.Owner = owner;
            owner.ReleaseFocus();
        }

        foreach (var (control, _) in snapshot.Modes)
        {
            if (GodotObject.IsInstanceValid(control))
            {
                control.FocusMode = Control.FocusModeEnum.None;
            }
        }

        _blocked[view] = snapshot;
    }

    private bool Unblock(Control view, HashSet<IViewModel> live)
    {
        if (!_blocked.Remove(view, out var snapshot))
        {
            return false;
        }

        if (!GodotObject.IsInstanceValid(view)
            || _presented.IsExiting(view)
            || !_presented.TryGetViewModel(view, out var viewModel)
            || !live.Contains(viewModel))
        {
            ViewFocus.ReleaseIfOwned(view);
            return false;
        }

        foreach (var (control, mode) in snapshot.Modes)
        {
            if (GodotObject.IsInstanceValid(control))
            {
                control.FocusMode = mode;
            }
        }

        if (snapshot.Owner is { } owner
            && GodotObject.IsInstanceValid(owner)
            && owner.IsInsideTree()
            && owner.FocusMode != Control.FocusModeEnum.None)
        {
            owner.GrabFocus();
        }

        return true;
    }

    private static void Capture(Control root, Snapshot snapshot)
    {
        snapshot.Modes.Add((root, root.FocusMode));
        foreach (var child in root.GetChildren())
        {
            if (child is Control control)
            {
                Capture(control, snapshot);
            }
        }
    }

    private sealed class Snapshot
    {
        public List<(Control Control, Control.FocusModeEnum Mode)> Modes { get; } = [];

        public Control? Owner { get; set; }
    }
}
