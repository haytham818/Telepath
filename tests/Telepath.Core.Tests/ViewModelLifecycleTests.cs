namespace Telepath.Core.Tests;

public sealed class ViewModelLifecycleTests
{
    [Fact]
    public void BoundAndUnboundHooksRunAroundDispose()
    {
        var viewModel = new RecordingViewModel();

        viewModel.NotifyBound();
        viewModel.NotifyUnbound();
        viewModel.Dispose();
        viewModel.NotifyBound();
        viewModel.NotifyUnbound();

        Assert.Equal(new[] { "bound", "unbound", "dispose" }, viewModel.Events);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var viewModel = new RecordingViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.Equal(new[] { "dispose" }, viewModel.Events);
    }

    private sealed class RecordingViewModel : ViewModel
    {
        public List<string> Events { get; } = [];

        protected override void OnBound() => Events.Add("bound");

        protected override void OnUnbound() => Events.Add("unbound");

        protected override void OnDispose() => Events.Add("dispose");
    }
}
