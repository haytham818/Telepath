namespace Telepath.Godot;

/// <summary>
/// Marker for the Godot integration assembly.
/// R3 Godot runtime glue (Frame/Time providers, Node/UI/Signal extensions) lives under
/// <c>addons/Telepath/Godot/R3/</c> in <c>namespace R3</c>; see doc/r3-godot.md.
/// </summary>
public static class TelepathGodot
{
    public const string Version = Telepath.Core.TelepathCore.Version;
}
