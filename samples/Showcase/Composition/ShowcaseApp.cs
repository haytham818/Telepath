using QFramework;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed class ShowcaseApp : Architecture<ShowcaseApp>
{
    protected override void Init() => RegisterUtility(new QfViewModelFactory());

    public static void BindShell(INavigator navigator, IOverlayHost overlay, IInteraction interaction)
    {
        Interface.RegisterUtility(new ShellServices(navigator, overlay, interaction));
    }
}
