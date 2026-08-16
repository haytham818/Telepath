using Godot;

namespace Telepath.Godot;

/// <summary>
/// Tracks in-flight enter / exit tasks for presenter-owned view nodes.
/// </summary>
internal sealed class ViewTransitionSession
{
    private readonly Dictionary<Control, CancellationTokenSource> _runs = [];

    public void PlayEnter(Control view)
    {
        var token = Start(view);
        ViewTransitionPlayback.Play(
            view,
            token,
            ViewTransition.PlayEnterAsync,
            completed => Complete(completed, token));
    }

    public void PlayExit(Control view, Action<Control> onFinished)
    {
        var token = Start(view);
        ViewTransitionPlayback.Play(
            view,
            token,
            ViewTransition.PlayExitAsync,
            completed =>
            {
                if (!Complete(completed, token))
                {
                    return;
                }

                onFinished(completed);
            });
    }

    public void Cancel(Control view)
    {
        if (_runs.Remove(view, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public void CancelAll()
    {
        foreach (var cts in _runs.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _runs.Clear();
    }

    private CancellationToken Start(Control view)
    {
        Cancel(view);
        var cts = new CancellationTokenSource();
        _runs[view] = cts;
        return cts.Token;
    }

    private bool Complete(Control view, CancellationToken token)
    {
        if (!_runs.TryGetValue(view, out var cts) || cts.Token != token)
        {
            return false;
        }

        _runs.Remove(view);
        cts.Dispose();
        return true;
    }
}
