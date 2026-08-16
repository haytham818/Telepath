namespace Telepath.Core.Tests;

public sealed class OverlayTests
{
    [Fact]
    public void PushDeactivatesCoveredAndActivatesOverlay()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        var panel = new Page("about", log);

        overlay.Push(panel);

        Assert.Same(panel, overlay.Layers[^1]);
        Assert.True(overlay.HasOverlay.Value);
        Assert.False(screen.IsDisposed);
        Assert.Equal(
            new[] { "screen:activate", "screen:deactivate", "about:activate" },
            log);
    }

    [Fact]
    public void PushStacksAndOnlyTopIsActive()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        var first = new Page("a", log);
        var second = new Page("b", log);

        overlay.Push(first);
        overlay.Push(second);

        Assert.Equal(2, overlay.Layers.Count);
        Assert.Same(second, overlay.Layers[^1]);
        Assert.False(first.IsDisposed);
        Assert.Equal(
            new[]
            {
                "screen:activate",
                "screen:deactivate",
                "a:activate",
                "a:deactivate",
                "b:activate"
            },
            log);
    }

    [Fact]
    public void BackRestoresPreviousOverlayWithoutDisposingCovered()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        var first = new Page("a", log);
        var second = new Page("b", log);
        overlay.Push(first);
        overlay.Push(second);
        log.Clear();

        Assert.True(overlay.Back());

        Assert.Same(first, overlay.Layers[^1]);
        Assert.True(second.IsDisposed);
        Assert.False(first.IsDisposed);
        Assert.False(screen.IsDisposed);
        Assert.Equal(new[] { "b:deactivate", "b:dispose", "a:activate" }, log);
    }

    [Fact]
    public void BackOnLastOverlayResumesCovered()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        var panel = new Page("about", log);
        overlay.Push(panel);
        log.Clear();

        Assert.True(overlay.Back());

        Assert.Empty(overlay.Layers);
        Assert.False(overlay.HasOverlay.Value);
        Assert.True(panel.IsDisposed);
        Assert.False(screen.IsDisposed);
        Assert.Equal(new[] { "about:deactivate", "about:dispose", "screen:activate" }, log);
    }

    [Fact]
    public void BackOnEmptyStackReturnsFalse()
    {
        using var overlay = new Overlay();
        Assert.False(overlay.Back());
        Assert.False(overlay.HasOverlay.Value);
    }

    [Fact]
    public void ClearWithoutResumeDoesNotActivateCovered()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        overlay.Push(new Page("about", log));
        log.Clear();

        overlay.Clear(resumeCovered: false);

        Assert.Empty(overlay.Layers);
        Assert.False(overlay.HasOverlay.Value);
        Assert.Equal(new[] { "about:deactivate", "about:dispose" }, log);
        Assert.False(screen.IsDisposed);
    }

    [Fact]
    public void ClearResumesCoveredByDefault()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var overlay = new Overlay(() => screen);
        overlay.Push(new Page("a", log));
        overlay.Push(new Page("b", log));
        log.Clear();

        overlay.Clear();

        Assert.Empty(overlay.Layers);
        Assert.Contains("screen:activate", log);
        Assert.Contains("b:dispose", log);
        Assert.Contains("a:dispose", log);
        Assert.False(screen.IsDisposed);
    }

    [Fact]
    public void CloseSelfPopsTop()
    {
        using var overlay = new Overlay();
        var first = new Page("a");
        var second = new Page("b");
        overlay.Push(first);
        overlay.Push(second);

        overlay.CloseSelf();

        Assert.Same(first, overlay.Layers[^1]);
        Assert.True(second.IsDisposed);
    }

    [Fact]
    public void CloseStackedLayerLeavesTopActive()
    {
        var log = new List<string>();
        using var overlay = new Overlay();
        var first = new Page("a", log);
        var second = new Page("b", log);
        overlay.Push(first);
        overlay.Push(second);
        log.Clear();

        overlay.Close(first);

        Assert.Same(second, overlay.Layers[^1]);
        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        Assert.Equal(new[] { "a:dispose" }, log);
    }

    [Fact]
    public void DisposeReleasesAllLayersWithoutResumingCovered()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        var overlay = new Overlay(() => screen);
        overlay.Push(new Page("a", log));
        overlay.Push(new Page("b", log));
        log.Clear();

        overlay.Dispose();

        Assert.True(overlay.IsDisposed);
        Assert.DoesNotContain("screen:activate", log);
        Assert.Contains("b:dispose", log);
        Assert.Contains("a:dispose", log);
        Assert.False(screen.IsDisposed);
        screen.Dispose();
    }

    [Fact]
    public void PushRejectsCoveredAndDuplicates()
    {
        var screen = new Page("screen");
        using var overlay = new Overlay(() => screen);
        var first = new Page("a");
        var second = new Page("b");
        overlay.Push(first);

        Assert.Throws<ArgumentException>(() => overlay.Push(screen));
        overlay.Push(first);
        Assert.Single(overlay.Layers);

        overlay.Push(second);
        Assert.Throws<InvalidOperationException>(() => overlay.Push(first));
        Assert.Equal(2, overlay.Layers.Count);
    }

    private sealed class Page : ViewModel, IActivatable
    {
        private readonly List<string> _log;
        private readonly string _name;

        public Page(string name, List<string>? log = null)
        {
            _name = name;
            _log = log ?? [];
        }

        public void Activate() => _log.Add($"{_name}:activate");

        public void Deactivate() => _log.Add($"{_name}:deactivate");

        protected override void OnDispose() => _log.Add($"{_name}:dispose");
    }
}
