using R3;

namespace Telepath.Core.Tests;

public sealed class ViewBindingTests
{
    [Fact]
    public void BindViewPresentsCurrentAndSubsequentViewModels()
    {
        using var source = new BindableReactiveProperty<IViewModel?>();
        var recording = new RecordingView();
        using var bindings = new BindingSet();
        using var first = new Page();
        using var second = new Page();

        bindings.BindView(source, recording.Target);
        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present:null" }, recording.Operations);

        source.Value = first;
        source.Value = second;

        Assert.Same(second, recording.Current);
        Assert.Equal(new[] { "present:null", "present", "present" }, recording.Operations);
    }

    [Fact]
    public void BindViewPresentsNull()
    {
        using var source = new BindableReactiveProperty<IViewModel?>();
        var recording = new RecordingView();
        using var bindings = new BindingSet();
        using var page = new Page();
        source.Value = page;
        bindings.BindView(source, recording.Target);

        source.Value = null;

        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present", "present:null" }, recording.Operations);
    }

    [Fact]
    public void BindViewDetachesWhenDisposed()
    {
        using var source = new BindableReactiveProperty<IViewModel?>();
        var recording = new RecordingView();
        var bindings = new BindingSet();
        using var page = new Page();
        source.Value = page;

        bindings.BindView(source, recording.Target);
        bindings.Dispose();
        source.Value = null;

        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present", "detach" }, recording.Operations);
    }

    [Fact]
    public void BindViewOneShotPresentsAndDetaches()
    {
        var recording = new RecordingView();
        var bindings = new BindingSet();
        using var page = new Page();

        bindings.BindView(page, recording.Target);
        Assert.Same(page, recording.Current);
        Assert.Equal(new[] { "present" }, recording.Operations);

        bindings.Dispose();
        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present", "detach" }, recording.Operations);
    }

    [Fact]
    public void BindViewOneShotAcceptsNull()
    {
        var recording = new RecordingView();
        using var bindings = new BindingSet();

        bindings.BindView((IViewModel?)null, recording.Target);

        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present:null" }, recording.Operations);
    }

    private sealed class Page : ViewModel;

    private sealed class RecordingView
    {
        public IViewModel? Current { get; private set; }

        public List<string> Operations { get; } = [];

        public ViewTarget Target => new(
            present: viewModel =>
            {
                Current = viewModel;
                Operations.Add(viewModel is null ? "present:null" : "present");
            },
            detach: () =>
            {
                Current = null;
                Operations.Add("detach");
            });
    }
}
