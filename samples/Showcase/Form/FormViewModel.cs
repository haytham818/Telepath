using System.Globalization;
using R3;
using Telepath.Core;

namespace Telepath.Showcase;

public sealed partial class FormViewModel : ViewModel
{
    [Bindable]
    private string _notes = "Hello from TextEdit.";

    [Bindable]
    private bool _subscribed = true;

    [Bindable]
    private bool _notifications = true;

    [Bindable]
    private bool _pinned = false;

    [Bindable]
    private bool _showAdvanced = true;

    [Bindable]
    private float _volume = 0.6f;

    [Bindable]
    private int _quantity = 3;

    [Bindable]
    private int _score = 42;

    [Bindable]
    private long _theme = 0;

    [Bindable]
    private bool _locked = false;

    [Bindable]
    private string _status = "Edit the form, then Submit.";

    [Bindable(nameof(Notes), nameof(Subscribed))]
    private string GetPreview(string notes, bool subscribed) =>
        $"[b]Preview[/b]\n{notes}\n\n[i]Subscribed: {(subscribed ? "yes" : "no")}[/i]";

    [Bindable(nameof(Volume))]
    private string FormatVolumeCaption(float volume) =>
        volume.ToString("0.00", CultureInfo.InvariantCulture);

    [Command(CanExecute = nameof(CanSubmit))]
    private void OnSubmit() =>
        Status.Value =
            $"Saved · theme {Theme.Value} · qty {Quantity.Value} · score {Score.Value} · vol {VolumeCaption.Value}";

    private Observable<bool> CanSubmit() => Locked.Select(static locked => !locked);

    [Command]
    private void OnEcho() =>
        Status.Value = Pinned.Value ? "Pinned echo." : "Echo.";

    [Command]
    private void OnApplyPreset(long index)
    {
        Volume.Value = index switch
        {
            0 => 0.2f,
            1 => 0.6f,
            _ => 1f,
        };
        Status.Value = index switch
        {
            0 => "Quiet preset.",
            1 => "Normal preset.",
            _ => "Loud preset.",
        };
    }
}
