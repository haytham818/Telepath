using R3;

namespace Telepath.Core.Tests;

public sealed class ConductorTests
{
    [Fact]
    public void NavigateSetsActiveItemAndActivates()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var page = new Page("a", log);

        conductor.Navigate(page);

        Assert.Same(page, conductor.ActiveItem.Value);
        Assert.Equal(new[] { "a:activate" }, log);
        Assert.False(conductor.CanGoBack.Value);
        Assert.False(conductor.BackCommand.CanExecute());
    }

    [Fact]
    public void NavigatePushesCurrentAndSwitchesActiveItem()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var first = new Page("a", log);
        var second = new Page("b", log);

        conductor.Navigate(first);
        conductor.Navigate(second);

        Assert.Same(second, conductor.ActiveItem.Value);
        Assert.True(conductor.CanGoBack.Value);
        Assert.True(conductor.BackCommand.CanExecute());
        Assert.False(first.IsDisposed);
        Assert.Equal(new[] { "a:activate", "a:deactivate", "b:activate" }, log);
    }

    [Fact]
    public void NavigateSameInstanceIsIgnored()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var page = new Page("a", log);

        conductor.Navigate(page);
        conductor.Navigate(page);

        Assert.Same(page, conductor.ActiveItem.Value);
        Assert.Equal(new[] { "a:activate" }, log);
    }

    [Fact]
    public void BackRestoresPreviousAndDisposesLeavingPage()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var first = new Page("a", log);
        var second = new Page("b", log);

        conductor.Navigate(first);
        conductor.Navigate(second);
        Assert.True(conductor.Back());

        Assert.Same(first, conductor.ActiveItem.Value);
        Assert.True(second.IsDisposed);
        Assert.False(first.IsDisposed);
        Assert.False(conductor.CanGoBack.Value);
        Assert.Equal(
            new[] { "a:activate", "a:deactivate", "b:activate", "b:deactivate", "b:dispose", "a:activate" },
            log);
    }

    [Fact]
    public void BackOnEmptyStackReturnsFalse()
    {
        using var conductor = new Conductor();
        var page = new Page("a");
        conductor.Navigate(page);

        Assert.False(conductor.Back());
        Assert.Same(page, conductor.ActiveItem.Value);
        Assert.False(page.IsDisposed);
    }

    [Fact]
    public void CloseActiveWithEmptyStackClearsSlot()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var page = new Page("a", log);
        conductor.Navigate(page);

        conductor.Close(page);

        Assert.Null(conductor.ActiveItem.Value);
        Assert.True(page.IsDisposed);
        Assert.Equal(new[] { "a:activate", "a:deactivate", "a:dispose" }, log);
    }

    [Fact]
    public void CloseActiveWithStackGoesBack()
    {
        using var conductor = new Conductor();
        var first = new Page("a");
        var second = new Page("b");
        conductor.Navigate(first);
        conductor.Navigate(second);

        conductor.CloseSelf();

        Assert.Same(first, conductor.ActiveItem.Value);
        Assert.True(second.IsDisposed);
        Assert.False(first.IsDisposed);
    }

    [Fact]
    public void CloseStackedPageDisposesItWithoutChangingActive()
    {
        using var conductor = new Conductor();
        var log = new List<string>();
        var first = new Page("a", log);
        var second = new Page("b", log);
        conductor.Navigate(first);
        conductor.Navigate(second);
        log.Clear();

        conductor.Close(first);

        Assert.Same(second, conductor.ActiveItem.Value);
        Assert.True(first.IsDisposed);
        Assert.False(second.IsDisposed);
        Assert.False(conductor.CanGoBack.Value);
        Assert.Equal(new[] { "a:dispose" }, log);
    }

    [Fact]
    public void DisposeReleasesActiveAndStackedPages()
    {
        var conductor = new Conductor();
        var first = new Page("a");
        var second = new Page("b");
        conductor.Navigate(first);
        conductor.Navigate(second);

        conductor.Dispose();

        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.True(conductor.IsDisposed);
    }

    [Fact]
    public void NavigateRejectsSelfAndDisposedAndStacked()
    {
        using var conductor = new Conductor();
        var first = new Page("a");
        var second = new Page("b");
        conductor.Navigate(first);
        conductor.Navigate(second);

        Assert.Throws<ArgumentException>(() => conductor.Navigate(conductor));
        Assert.Throws<InvalidOperationException>(() => conductor.Navigate(first));

        first.Dispose();
        using var leftover = new Conductor();
        Assert.Throws<ObjectDisposedException>(() => leftover.Navigate(first));
    }

    [Fact]
    public void CloseUnknownPageThrows()
    {
        using var conductor = new Conductor();
        conductor.Navigate(new Page("a"));
        using var orphan = new Page("orphan");

        Assert.Throws<InvalidOperationException>(() => conductor.Close(orphan));
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
