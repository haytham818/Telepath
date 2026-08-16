#if TOOLS
using Godot;

namespace Telepath.Godot.Editor;

internal sealed record ControlInfo(string Path, string TypeName, bool HasUniqueName);

internal static class ControlCatalog
{
    public static IReadOnlyList<ControlInfo> Collect(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);
        var controls = new List<ControlInfo>();
        CollectRecursive(root, root, controls);
        return controls;
    }

    private static void CollectRecursive(Node root, Node current, List<ControlInfo> controls)
    {
        if (!ReferenceEquals(current, root) && current is Control)
        {
            var unique = current.UniqueNameInOwner;
            var path = unique ? "%" + current.Name : root.GetPathTo(current).ToString();
            controls.Add(new ControlInfo(path, current.GetType().Name, unique));
        }

        foreach (var child in current.GetChildren())
        {
            CollectRecursive(root, child, controls);
        }
    }
}
#endif
