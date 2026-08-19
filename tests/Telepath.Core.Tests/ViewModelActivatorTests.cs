namespace Telepath.Core.Tests;

public sealed class ViewModelActivatorTests
{
    [Fact]
    public void NavigateTCreatesAndOwnsThePage()
    {
        using var conductor = new Conductor
        {
            ViewModelActivator = new ReflectionActivator()
        };

        conductor.Navigate<NamedPage>("home");

        var page = Assert.IsType<NamedPage>(conductor.ActiveItem.Value);
        Assert.Equal("home", page.Name);

        conductor.Dispose();
        Assert.True(page.IsDisposed);
    }

    [Fact]
    public void NavigateTWithoutActivatorThrows()
    {
        using var conductor = new Conductor();
        Assert.Throws<InvalidOperationException>(() => conductor.Navigate<BlankPage>());
    }

    [Fact]
    public void OverlayPushTCreatesAndOwnsThePanel()
    {
        using var overlay = new Overlay
        {
            ViewModelActivator = new ReflectionActivator()
        };

        overlay.Push<NamedPage>(CoverMode.Continue, "about");

        var panel = Assert.IsType<NamedPage>(overlay.Layers[^1]);
        Assert.Equal("about", panel.Name);

        overlay.Dispose();
        Assert.True(panel.IsDisposed);
    }

    [Fact]
    public void OverlayPushTWithoutActivatorThrows()
    {
        using var overlay = new Overlay();
        Assert.Throws<InvalidOperationException>(() => overlay.Push<BlankPage>());
    }

    [Fact]
    public void OverlayHostPushTGoesToTheRequestedBand()
    {
        using var host = new OverlayHost
        {
            ViewModelActivator = new ReflectionActivator()
        };

        host.Push<BlankPage>(OverlayLayer.Modal);

        Assert.IsType<BlankPage>(host.Band(OverlayLayer.Modal).Layers[^1]);
        Assert.Empty(host.Band(OverlayLayer.Popup).Layers);
    }

    [Fact]
    public void OverlayHostActivatorPropagatesToBands()
    {
        using var host = new OverlayHost
        {
            ViewModelActivator = new ReflectionActivator()
        };

        host.Band(OverlayLayer.Toast).Push<BlankPage>();

        Assert.IsType<BlankPage>(host.Band(OverlayLayer.Toast).Layers[^1]);
    }

    private sealed class BlankPage : ViewModel;

    private sealed class NamedPage : ViewModel
    {
        public NamedPage(string name) => Name = name;

        public string Name { get; }
    }

    private sealed class ReflectionActivator : IViewModelActivator
    {
        public T Create<T>(params object[] arguments) where T : class, IViewModel
            => (T)(Activator.CreateInstance(typeof(T), arguments)
                ?? throw new InvalidOperationException($"Failed to create '{typeof(T).Name}'."));
    }
}
