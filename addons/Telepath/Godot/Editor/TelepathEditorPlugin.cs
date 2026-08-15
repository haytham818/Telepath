#if TOOLS
using Godot;

namespace Telepath.Godot;

[Tool]
public partial class TelepathEditorPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
    }
}
#endif
