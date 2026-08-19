#if TOOLS
#nullable enable
using Godot;

namespace Telepath.Godot.Editor;

/// <summary>
/// Godot snapshots collectible <see cref="Callable"/>s before
/// <see cref="ISerializationListener.OnBeforeSerialize"/>. Leftover
/// <c>Delegate::Invoke</c> connections from older C# docks are dropped here.
/// </summary>
internal static class StaleCallableCleanup
{
    public static void DropInvalidTree(Node root)
    {
        if (!GodotObject.IsInstanceValid(root))
        {
            return;
        }

        DropInvalid(root);
        foreach (var child in root.GetChildren())
        {
            DropInvalidTree(child);
        }
    }

    public static void DropInvalid(GodotObject source)
    {
        if (!GodotObject.IsInstanceValid(source))
        {
            return;
        }

        foreach (var signalInfo in source.GetSignalList())
        {
            var signal = signalInfo["name"].AsStringName();
            foreach (var connection in source.GetSignalConnectionList(signal))
            {
                var callable = connection["callable"].AsCallable();
                if (!ShouldDrop(callable))
                {
                    continue;
                }

                if (!source.IsConnected(signal, callable))
                {
                    continue;
                }

                try
                {
                    source.Disconnect(signal, callable);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private static bool ShouldDrop(Callable callable)
    {
        // C# `+=` / FromEvent wrappers. They cannot retarget across ALC reload.
        if (callable.Method == (StringName)"Invoke")
        {
            return true;
        }

        return callable.Target is GodotObject target && !GodotObject.IsInstanceValid(target);
    }
}
#endif
