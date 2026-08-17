using R3;

namespace Telepath.Core;

/// <summary>
/// Built-in yes/no dialog. Hosts register a scene for this type; Back and
/// cancel complete as <see langword="false"/>.
/// </summary>
public sealed class ConfirmViewModel : DialogViewModel<bool>
{
    public ConfirmViewModel(string title, string message)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        Title = Track(new BindableReactiveProperty<string>(title));
        Message = Track(new BindableReactiveProperty<string>(message));
        YesCommand = Command(() => Complete(true));
        NoCommand = Command(() => Complete(false));
    }

    public BindableReactiveProperty<string> Title { get; }

    public BindableReactiveProperty<string> Message { get; }

    public ReactiveCommand YesCommand { get; }

    public ReactiveCommand NoCommand { get; }

    /// <inheritdoc />
    protected override bool Dismissed => false;
}
