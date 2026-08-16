using Telepath.Core;

namespace Telepath.Showcase.Shell;

public sealed partial class StatusViewModel : ViewModel
{
    [Bindable]
    private string _caption = "Showcase";
}
