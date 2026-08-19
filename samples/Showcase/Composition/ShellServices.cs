using QFramework;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed class ShellServices : IUtility
{
    public ShellServices(INavigator navigator, IOverlayHost overlay, IInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(overlay);
        ArgumentNullException.ThrowIfNull(interaction);
        Navigator = navigator;
        Overlay = overlay;
        Interaction = interaction;
    }

    public INavigator Navigator { get; }

    public IOverlayHost Overlay { get; }

    public IInteraction Interaction { get; }
}
