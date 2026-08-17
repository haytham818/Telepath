using Telepath.Core;

namespace Telepath.Core.Tests;

public sealed class InteractionTests
{
    [Fact]
    public async Task RunCompletesCustomDialog()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt);

        Assert.Same(prompt, host.Band(OverlayLayer.Modal).Layers[^1]);
        Assert.Empty(host.Layers);
        Assert.True(host.HasBackableOverlay.Value);
        prompt.Accept("picked");

        Assert.Equal("picked", await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(prompt.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        Assert.False(host.HasOverlay.Value);
    }

    [Fact]
    public async Task RunPushesRequestedLayer()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt, OverlayLayer.Popup);

        Assert.Same(prompt, host.Band(OverlayLayer.Popup).Layers[^1]);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        prompt.Accept("popup");

        Assert.Equal("popup", await task.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task RunBackUsesDismissedValue()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt);

        Assert.True(host.Back());

        Assert.Equal("", await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(prompt.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task ClearUsesDismissedValue()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt);

        host.Clear();

        Assert.Equal("", await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(prompt.IsDisposed);
    }

    [Fact]
    public async Task CancelledTokenUsesDismissedValue()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        using var cts = new CancellationTokenSource();
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt, cancellationToken: cts.Token);

        cts.Cancel();

        Assert.Equal("", await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(prompt.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task AlreadyCancelledTokenUsesDismissedValue()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var prompt = new PromptDialog();

        var result = await interaction
            .Run(prompt, cancellationToken: cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("", result);
        Assert.True(prompt.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task BoolDialogCompleteTrueReturnsTrue()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var dialog = new BoolDialog();
        var task = interaction.Run(dialog);

        dialog.Accept(true);

        Assert.True(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(dialog.IsDisposed);
    }

    private sealed class PromptDialog : DialogViewModel<string>
    {
        protected override string Dismissed => "";

        public void Accept(string value) => Complete(value);
    }

    private sealed class BoolDialog : DialogViewModel<bool>
    {
        protected override bool Dismissed => false;

        public void Accept(bool value) => Complete(value);
    }
}
