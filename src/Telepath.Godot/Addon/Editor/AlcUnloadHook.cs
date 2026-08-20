#if TOOLS
#nullable enable
using System.Runtime.Loader;
using System.Threading;
using R3;

namespace Telepath.Godot.Editor;

internal static class AlcUnloadHook
{
    private static int _registered;

    internal static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        var context = AssemblyLoadContext.GetLoadContext(typeof(AlcUnloadHook).Assembly);
        if (context is null || ReferenceEquals(context, AssemblyLoadContext.Default))
        {
            return;
        }

        context.Unloading += static _ =>
        {
            try
            {
                ConverterCatalog.Clear();
                GodotProviderInitializer.ResetObservableSystem();
            }
            catch
            {
            }
        };
    }
}
#endif
