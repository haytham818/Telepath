using Godot;

namespace Telepath.Godot;

internal static class ViewFocus
{
    public static void Take(Control view)
    {
        if (!GodotObject.IsInstanceValid(view) || !view.IsInsideTree())
        {
            return;
        }

        if (view is IViewFocus custom)
        {
            custom.TakeFocus();
            return;
        }

        FindFirstFocusable(view)?.GrabFocus();
    }

    public static Control? FindFirstFocusable(Control root)
    {
        if (root.FocusMode != Control.FocusModeEnum.None)
        {
            return root;
        }

        foreach (var child in root.GetChildren())
        {
            if (child is not Control control)
            {
                continue;
            }

            var found = FindFirstFocusable(control);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public static void ReleaseIfOwned(Control view)
    {
        if (!GodotObject.IsInstanceValid(view))
        {
            return;
        }

        var owner = view.GetViewport()?.GuiGetFocusOwner();
        if (owner is not null && IsDescendantOrSelf(view, owner))
        {
            owner.ReleaseFocus();
        }
    }

    public static bool IsDescendantOrSelf(Control root, Control node)
    {
        Control? current = node;
        while (current is not null)
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }

            current = current.GetParent() as Control;
        }

        return false;
    }
}
