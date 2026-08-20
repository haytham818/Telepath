using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps ViewModel types to stable Godot scene UIDs and instantiates their views on demand.
/// Filled explicitly by the host; does not scan <c>[TelepathView]</c> or retain loaded scenes.
/// </summary>
public sealed class ViewRegistry
{
    private readonly Dictionary<Type, long> _sceneUids = [];

    public ViewRegistry Register<TViewModel>(string sceneUid)
        where TViewModel : class, IViewModel
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneUid);
        if (!sceneUid.StartsWith("uid://", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Scene reference '{sceneUid}' must be a Godot resource UID.",
                nameof(sceneUid));
        }

        var uid = ResourceUid.TextToId(sceneUid);
        if (uid == ResourceUid.InvalidId || !ResourceUid.HasId(uid))
        {
            throw new ArgumentException(
                $"Scene UID '{sceneUid}' is not registered in the current Godot project.",
                nameof(sceneUid));
        }

        return Register(typeof(TViewModel), uid);
    }

    public Control Create(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        if (!_sceneUids.TryGetValue(viewModelType, out var uid))
        {
            throw new InvalidOperationException(
                $"No scene is registered for ViewModel type '{viewModelType.FullName}'.");
        }

        var sceneUid = ResourceUid.IdToText(uid);
        if (!ResourceUid.HasId(uid))
        {
            throw new InvalidOperationException(
                $"Scene UID '{sceneUid}' registered for ViewModel type " +
                $"'{viewModelType.FullName}' is no longer available.");
        }

        var scenePath = ResourceUid.GetIdPath(uid);
        var scene = ResourceLoader.Load<PackedScene>(scenePath, cacheMode: ResourceLoader.CacheMode.Reuse)
            ?? throw new InvalidOperationException(
                $"Failed to load scene '{sceneUid}' for ViewModel type " +
                $"'{viewModelType.FullName}'.");
        return scene.Instantiate<Control>();
    }

    private ViewRegistry Register(Type viewModelType, long sceneUid)
    {
        if (!_sceneUids.TryAdd(viewModelType, sceneUid))
        {
            throw new InvalidOperationException(
                $"A scene is already registered for ViewModel type '{viewModelType.FullName}'.");
        }

        return this;
    }
}
