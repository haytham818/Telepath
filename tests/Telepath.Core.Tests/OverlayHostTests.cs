namespace Telepath.Core.Tests;

public sealed class OverlayHostTests
{
    [Fact]
    public void DefaultPushGoesToPopup()
    {
        using var host = new OverlayHost();
        var panel = new Page("about");

        host.Push(panel);

        Assert.Same(panel, host.Layers[^1]);
        Assert.Same(panel, host.Band(OverlayLayer.Popup).Layers[^1]);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        Assert.True(host.HasOverlay.Value);
        Assert.True(host.HasBackableOverlay.Value);
    }

    [Fact]
    public void BuiltInBandsAreOrderedPopupModalToast()
    {
        using var host = new OverlayHost();
        Assert.Equal(
            new[] { OverlayLayer.Popup, OverlayLayer.Modal, OverlayLayer.Toast },
            host.Bands);
    }

    [Fact]
    public void ToastStaysAboveModalAndDoesNotPause()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var modal = new Page("modal", log);
        var toast = new Page("toast", log);

        host.Push(modal, OverlayLayer.Modal);
        host.Push(toast, OverlayLayer.Toast);

        Assert.Same(modal, host.Band(OverlayLayer.Modal).Layers[^1]);
        Assert.Same(toast, host.Band(OverlayLayer.Toast).Layers[^1]);
        Assert.Equal(
            new[]
            {
                "screen:activate",
                "screen:deactivate",
                "modal:activate",
                "toast:activate"
            },
            log);
    }

    [Fact]
    public void BackSkipsToastAndPopsHighestBackableBand()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var popup = new Page("popup", log);
        var modal = new Page("modal", log);
        var toast = new Page("toast", log);
        host.Push(popup);
        host.Push(modal, OverlayLayer.Modal);
        host.Push(toast, OverlayLayer.Toast);
        log.Clear();

        Assert.True(host.Back());

        Assert.Same(toast, host.Band(OverlayLayer.Toast).Layers[^1]);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        Assert.True(modal.IsDisposed);
        Assert.False(toast.IsDisposed);
        Assert.Equal(new[] { "modal:deactivate", "popup:activate", "modal:dispose" }, log);
        Assert.True(host.HasOverlay.Value);
        Assert.True(host.HasBackableOverlay.Value);

        log.Clear();
        Assert.True(host.Back());
        Assert.Empty(host.Band(OverlayLayer.Popup).Layers);
        Assert.True(popup.IsDisposed);
        Assert.Same(toast, host.Band(OverlayLayer.Toast).Layers[^1]);
        Assert.True(host.HasOverlay.Value);
        Assert.False(host.HasBackableOverlay.Value);
        Assert.False(host.Back());
    }

    [Fact]
    public void PushOntoLowerBandUnderPauseModalDoesNotActivate()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var modal = new Page("modal", log);
        host.Push(modal, OverlayLayer.Modal);
        log.Clear();

        var popup = new Page("popup", log);
        host.Push(popup);

        Assert.Same(popup, host.Band(OverlayLayer.Popup).Layers[^1]);
        Assert.False(popup.IsDisposed);
        Assert.DoesNotContain("popup:activate", log);
        Assert.Equal(Array.Empty<string>(), log);
    }

    [Fact]
    public void RegisterInsertsBetweenBuiltIns()
    {
        using var host = new OverlayHost();
        var banner = new OverlayLayer(
            "Banner",
            order: 50,
            handlesBack: false,
            defaultCover: CoverMode.Continue,
            blocksPassThrough: false);

        host.Register(banner);

        Assert.Equal(
            new[]
            {
                OverlayLayer.Popup,
                banner,
                OverlayLayer.Modal,
                OverlayLayer.Toast
            },
            host.Bands);
    }

    [Fact]
    public void RegisterDuplicateNameThrows()
    {
        using var host = new OverlayHost();
        Assert.Throws<InvalidOperationException>(() =>
            host.Register(new OverlayLayer("Popup", order: 50)));
    }

    [Fact]
    public void RegisterDuplicateOrderThrows()
    {
        using var host = new OverlayHost();
        Assert.Throws<InvalidOperationException>(() =>
            host.Register(new OverlayLayer("Banner", order: 100)));
    }

    [Fact]
    public void RegisterAfterPushThrows()
    {
        using var host = new OverlayHost();
        host.Push(new Page("about"));
        Assert.Throws<InvalidOperationException>(() =>
            host.Register(new OverlayLayer("Banner", order: 50)));
    }

    [Fact]
    public void PushUnknownLayerThrows()
    {
        using var host = new OverlayHost();
        Assert.Throws<InvalidOperationException>(() =>
            host.Push(new Page("x"), new OverlayLayer("Loot", order: 40)));
    }

    [Fact]
    public void ToastOnlyDoesNotHandleBack()
    {
        using var host = new OverlayHost();
        host.Push(new Page("toast"), OverlayLayer.Toast);

        Assert.True(host.HasOverlay.Value);
        Assert.False(host.HasBackableOverlay.Value);
        Assert.False(host.Back());
        Assert.Single(host.Band(OverlayLayer.Toast).Layers);
    }

    [Fact]
    public void CloseFindsViewModelAcrossBands()
    {
        using var host = new OverlayHost();
        var modal = new Page("modal");
        host.Push(modal, OverlayLayer.Modal);

        host.Close(modal);

        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        Assert.True(modal.IsDisposed);
        Assert.False(host.HasOverlay.Value);
    }

    [Fact]
    public void ClearClosesEveryBandAndResumesCovered()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        host.Push(new Page("popup", log));
        host.Push(new Page("toast", log), OverlayLayer.Toast);
        log.Clear();

        host.Clear();

        Assert.Empty(host.Band(OverlayLayer.Popup).Layers);
        Assert.Empty(host.Band(OverlayLayer.Toast).Layers);
        Assert.Contains("toast:deactivate", log);
        Assert.Contains("popup:deactivate", log);
        Assert.Contains("screen:activate", log);
        Assert.False(host.HasOverlay.Value);
        Assert.Empty(host.Covered.Value);
    }

    [Fact]
    public void ClearWithoutResumeLeavesScreenPaused()
    {
        var log = new List<string>();
        var screen = new Page("screen", log);
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        host.Push(new Page("popup", log));
        log.Clear();

        host.Clear(resumeCovered: false);

        Assert.DoesNotContain("screen:activate", log);
        Assert.False(screen.IsDisposed);
        Assert.Contains(screen, host.Covered.Value);
    }

    [Fact]
    public void ToastCoversScreenWithoutPausing()
    {
        var screen = new Page("screen");
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var toast = new Page("toast");

        host.Push(toast, OverlayLayer.Toast);

        Assert.Equal(new IViewModel[] { screen }, host.Covered.Value);
        Assert.Same(screen, host.CurrentScreen);
    }

    [Fact]
    public void ModalCoversScreenAndPopup()
    {
        var screen = new Page("screen");
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var popup = new Page("popup");
        var modal = new Page("modal");
        host.Push(popup);
        host.Push(modal, OverlayLayer.Modal);

        Assert.Equal(new IViewModel[] { screen, popup }, host.Covered.Value);
    }

    [Fact]
    public void SameBandPushCoversLowerOverlay()
    {
        var screen = new Page("screen");
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var lower = new Page("lower");
        var upper = new Page("upper");
        host.Push(lower);
        host.Push(upper);

        Assert.Equal(new IViewModel[] { screen, lower }, host.Covered.Value);

        host.Back();

        Assert.Equal(new IViewModel[] { screen }, host.Covered.Value);
    }

    [Fact]
    public void BackRevealsCoveredPage()
    {
        var screen = new Page("screen");
        screen.Activate();
        using var host = new OverlayHost(() => screen);
        var popup = new Page("popup");
        host.Push(popup);
        Assert.Equal(new IViewModel[] { screen }, host.Covered.Value);

        host.Back();

        Assert.Empty(host.Covered.Value);
    }

    [Fact]
    public void ClearWithoutResumeKeepsScreenCoveredUntilScreenChanges()
    {
        var screen = new Page("screen");
        var next = new Page("next");
        IViewModel current = screen;
        screen.Activate();
        using var host = new OverlayHost(() => current);
        host.Push(new Page("popup"));

        host.Clear(resumeCovered: false);
        Assert.Contains(screen, host.Covered.Value);

        current = next;
        host.Clear();

        Assert.DoesNotContain(screen, host.Covered.Value);
        Assert.Empty(host.Covered.Value);
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
