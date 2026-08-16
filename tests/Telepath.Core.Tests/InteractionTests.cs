using R3;

namespace Telepath.Core.Tests;

public sealed class InteractionTests
{
    [Fact]
    public async Task ConfirmYesReturnsTrueAndPushesModal()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var task = interaction.Confirm("Delete", "Remove this item?");
        var dialog = Assert.IsType<ConfirmViewModel>(host.Band(OverlayLayer.Modal).Layers[^1]);

        Assert.Equal("Delete", dialog.Title.Value);
        Assert.Equal("Remove this item?", dialog.Message.Value);
        Assert.Empty(host.Layers);
        Assert.True(host.HasBackableOverlay.Value);

        dialog.YesCommand.Execute(Unit.Default);

        Assert.True(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
        Assert.True(dialog.IsDisposed);
        Assert.False(host.HasOverlay.Value);
    }

    [Fact]
    public async Task ConfirmNoReturnsFalse()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var task = interaction.Confirm("Delete", "Remove this item?");
        var dialog = Assert.IsType<ConfirmViewModel>(host.Band(OverlayLayer.Modal).Layers[^1]);

        dialog.NoCommand.Execute(Unit.Default);

        Assert.False(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(dialog.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task BackDismissesConfirmAsFalse()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var task = interaction.Confirm("Delete", "Remove this item?");
        var dialog = Assert.IsType<ConfirmViewModel>(host.Band(OverlayLayer.Modal).Layers[^1]);

        Assert.True(host.Back());

        Assert.False(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(dialog.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task ClearDismissesConfirmAsFalse()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var task = interaction.Confirm("Delete", "Remove this item?");
        var dialog = Assert.IsType<ConfirmViewModel>(host.Band(OverlayLayer.Modal).Layers[^1]);

        host.Clear();

        Assert.False(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(dialog.IsDisposed);
    }

    [Fact]
    public async Task CancelledTokenDismissesConfirmAsFalse()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        using var cts = new CancellationTokenSource();
        var task = interaction.Confirm("Delete", "Remove this item?", cts.Token);
        var dialog = Assert.IsType<ConfirmViewModel>(host.Band(OverlayLayer.Modal).Layers[^1]);

        cts.Cancel();

        Assert.False(await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(dialog.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task AlreadyCancelledTokenDismissesConfirmAsFalse()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await interaction
            .Confirm("Delete", "Remove this item?", cts.Token)
            .WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(result);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
    }

    [Fact]
    public async Task RunCompletesCustomDialog()
    {
        using var host = new OverlayHost();
        var interaction = new Interaction(host);
        var prompt = new PromptDialog();
        var task = interaction.Run(prompt);

        Assert.Same(prompt, host.Band(OverlayLayer.Modal).Layers[^1]);
        prompt.Accept("picked");

        Assert.Equal("picked", await task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(prompt.IsDisposed);
        Assert.Empty(host.Band(OverlayLayer.Modal).Layers);
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
    }

    private sealed class PromptDialog : DialogViewModel<string>
    {
        protected override string Dismissed => "";

        public void Accept(string value) => Complete(value);
    }
}
