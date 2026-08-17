namespace Telepath.Core;

/// <summary>
/// Named overlay band. Bands are independent stacks; <see cref="Order"/> is the
/// z-order between bands, not a slot in a single stack.
/// </summary>
public readonly struct OverlayLayer : IEquatable<OverlayLayer>
{
    /// <summary>
    /// Default dialogs and panels. Bottom built-in band.
    /// </summary>
    public static OverlayLayer Popup { get; } = new(
        "Popup",
        order: 0,
        handlesBack: true,
        defaultCover: CoverMode.Pause,
        blocksPassThrough: true);

    /// <summary>
    /// Blocking dialogs above <see cref="Popup"/>.
    /// </summary>
    public static OverlayLayer Modal { get; } = new(
        "Modal",
        order: 100,
        handlesBack: true,
        defaultCover: CoverMode.Pause,
        blocksPassThrough: true);

    /// <summary>
    /// Notifications above <see cref="Modal"/>. Does not handle Back and does
    /// not swallow clicks on empty space.
    /// </summary>
    public static OverlayLayer Toast { get; } = new(
        "Toast",
        order: 200,
        handlesBack: false,
        defaultCover: CoverMode.Continue,
        blocksPassThrough: false);

    public OverlayLayer(
        string name,
        int order,
        bool handlesBack = true,
        CoverMode defaultCover = CoverMode.Pause,
        bool blocksPassThrough = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Order = order;
        HandlesBack = handlesBack;
        DefaultCover = defaultCover;
        BlocksPassThrough = blocksPassThrough;
    }

    /// <summary>
    /// Identity of the band. Lookups match on name, not order.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Z-order between bands. Higher draws above. Leave gaps of 100 so apps can insert.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// When <see langword="true"/>, host <see cref="IOverlay.Back"/> may pop this band.
    /// </summary>
    public bool HandlesBack { get; }

    /// <summary>
    /// Used when <see cref="IOverlayHost.Push(IViewModel, OverlayLayer, CoverMode?)"/>
    /// omits a cover mode.
    /// </summary>
    public CoverMode DefaultCover { get; }

    /// <summary>
    /// When <see langword="true"/>, a non-empty Godot slot swallows clicks
    /// (<c>MouseFilter.Stop</c>) and views underneath enter
    /// <see cref="IOverlayHost.InputBlocked"/>. Toast-style bands keep the
    /// slot ignoring and do not steal keyboard focus.
    /// </summary>
    public bool BlocksPassThrough { get; }

    public bool Equals(OverlayLayer other) =>
        string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is OverlayLayer other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

    public override string ToString() => Name;

    public static bool operator ==(OverlayLayer left, OverlayLayer right) => left.Equals(right);

    public static bool operator !=(OverlayLayer left, OverlayLayer right) => !left.Equals(right);
}
