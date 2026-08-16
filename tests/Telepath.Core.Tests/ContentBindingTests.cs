using R3;

namespace Telepath.Core.Tests;

public sealed class ContentBindingTests
{
    [Fact]
    public void BindContentPresentsCurrentAndSubsequentPages()
    {
        using var source = new BindableReactiveProperty<IViewModel?>();
        var recording = new RecordingContent();
        using var bindings = new BindingSet();
        using var first = new Page();
        using var second = new Page();

        bindings.BindContent(source, recording.Target);
        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present:null" }, recording.Operations);

        source.Value = first;
        source.Value = second;

        Assert.Same(second, recording.Current);
        Assert.Equal(new[] { "present:null", "present", "present" }, recording.Operations);
    }

    [Fact]
    public void BindContentDetachesWhenDisposed()
    {
        using var source = new BindableReactiveProperty<IViewModel?>();
        var recording = new RecordingContent();
        var bindings = new BindingSet();
        using var page = new Page();
        source.Value = page;

        bindings.BindContent(source, recording.Target);
        bindings.Dispose();
        source.Value = null;

        Assert.Null(recording.Current);
        Assert.Equal(new[] { "present", "detach" }, recording.Operations);
    }

    private sealed class Page : ViewModel;

    private sealed class RecordingContent
    {
        public IViewModel? Current { get; private set; }

        public List<string> Operations { get; } = [];

        public ContentTarget Target => new(
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
