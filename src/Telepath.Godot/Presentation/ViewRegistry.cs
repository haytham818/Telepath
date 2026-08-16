using Godot;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Maps ViewModel types to the <see cref="PackedScene"/> that presents them.
/// Filled explicitly by the host; does not scan <c>[TelepathView]</c>.
/// </summary>
public sealed class ViewRegistry
{
    private readonly Dictionary<Type, PackedScene> _scenes = [];

    public ViewRegistry Register<TViewModel>(PackedScene scene)
        where TViewModel : class, IViewModel
    {
        ArgumentNullException.ThrowIfNull(scene);
        return Register(typeof(TViewModel), scene);
    }

    public ViewRegistry Register<TViewModel>(string scenePath)
        where TViewModel : class, IViewModel
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        var scene = GD.Load<PackedScene>(scenePath)
            ?? throw new InvalidOperationException($"Failed to load scene '{scenePath}'.");
        return Register<TViewModel>(scene);
    }

    public PackedScene Resolve(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        if (_scenes.TryGetValue(viewModelType, out var scene))
        {
            return scene;
        }

        throw new InvalidOperationException(
            $"No scene is registered for ViewModel type '{viewModelType.FullName}'.");
    }

    private ViewRegistry Register(Type viewModelType, PackedScene scene)
    {
        if (!_scenes.TryAdd(viewModelType, scene))
        {
            throw new InvalidOperationException(
                $"A scene is already registered for ViewModel type '{viewModelType.FullName}'.");
        }

        return this;
    }
}
