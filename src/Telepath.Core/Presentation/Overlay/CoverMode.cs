namespace Telepath.Core;

/// <summary>
/// How an overlay treats the page or overlay it covers.
/// </summary>
public enum CoverMode
{
    /// <summary>
    /// <see cref="IActivatable.Deactivate"/> the covered item. Closing this
    /// overlay activates it again.
    /// </summary>
    Pause,

    /// <summary>
    /// Leave the covered item running. Closing this overlay does not activate it.
    /// </summary>
    Continue
}
