using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class ConfirmViewModel : DialogViewModel<bool>
{
    [Bindable]
    private string _title;

    [Bindable]
    private string _message;

    public ConfirmViewModel(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        _title = title;
        _message = message;
    }

    [Command]
    private void OnYes() => Complete(true);

    [Command]
    private void OnNo() => Complete(false);

    protected override bool Dismissed => false;
}
