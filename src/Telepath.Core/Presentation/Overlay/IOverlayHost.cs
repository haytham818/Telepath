using R3;

namespace Telepath.Core;

/// <summary>
/// Named overlay bands. Each band is an independent <see cref="IOverlay"/> stack.
/// Parameterless <see cref="IOverlay"/> members target <see cref="OverlayLayer.Popup"/>.
/// </summary>
public interface IOverlayHost : IOverlay
{
    /// <summary>
    /// Registered bands from lowest <see cref="OverlayLayer.Order"/> to highest.
    /// </summary>
    IReadOnlyList<OverlayLayer> Bands { get; }

    /// <summary>
    /// <see langword="true"/> when a band that handles Back has at least one overlay.
    /// </summary>
    BindableReactiveProperty<bool> HasBackableOverlay { get; }

    /// <summary>
    /// The screen under all overlay bands, from the host constructor callback.
    /// </summary>
    IViewModel? CurrentScreen { get; }

    /// <summary>
    /// ViewModels that currently have at least one overlay stacked above them,
    /// in z-order from bottom to top. Includes the screen and lower overlay
    /// layers. Independent of <see cref="CoverMode"/>: a Continue toast still
    /// covers whatever sits below it.
    /// </summary>
    BindableReactiveProperty<IReadOnlyList<IViewModel>> Covered { get; }

    /// <summary>
    /// ViewModels that currently have at least one
    /// <see cref="OverlayLayer.BlocksPassThrough"/> overlay stacked above them,
    /// in z-order from bottom to top. Independent of <see cref="CoverMode"/>.
    /// A toast covers visually but does not belong here.
    /// </summary>
    BindableReactiveProperty<IReadOnlyList<IViewModel>> InputBlocked { get; }

    /// <summary>
    /// Registers a custom band. Throws after the first overlay is pushed,
    /// or when <see cref="OverlayLayer.Name"/> / <see cref="OverlayLayer.Order"/> collides.
    /// </summary>
    void Register(OverlayLayer layer);

    /// <summary>
    /// The stack for <paramref name="layer"/>. Throws when the band is unknown.
    /// </summary>
    IOverlay Band(OverlayLayer layer);

    /// <summary>
    /// Pushes onto <paramref name="layer"/>. When <paramref name="cover"/> is
    /// omitted, <see cref="OverlayLayer.DefaultCover"/> is used.
    /// </summary>
    void Push(IViewModel viewModel, OverlayLayer layer, CoverMode? cover = null);

    /// <summary>
    /// Creates <typeparamref name="T"/> with <see cref="IViewModelActivator"/> and
    /// pushes it onto <paramref name="layer"/>. Requires <c>ViewModelActivator</c>
    /// on the host.
    /// </summary>
    void Push<T>(OverlayLayer layer, CoverMode? cover = null, params object[] arguments)
        where T : class, IViewModel;
}
