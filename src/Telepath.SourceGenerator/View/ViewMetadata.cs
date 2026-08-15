using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal static class ViewMetadata
{
    public const string ViewAttributeName = "Telepath.Godot.TelepathViewAttribute`1";
    public const string LinkToAttributeName = "Telepath.Godot.LinkToAttribute";
    public const string ControlName = "Godot.Control";
    public const string LabelName = "Godot.Label";
    public const string BaseButtonName = "Godot.BaseButton";
    public const string ViewModelName = "Telepath.Core.IViewModel";
    public const string BindingSetName = "Telepath.Godot.BindingSet";
    public const string DiagnosticCategory = "Telepath.View";

    public static readonly DiagnosticDescriptor InvalidTarget = new(
        id: "TPV001",
        title: "Invalid Telepath view target",
        messageFormat: "Telepath view '{0}' must be a non-generic partial class derived from Godot.Control",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingNotificationBridge = new(
        id: "TPV002",
        title: "Missing Godot notification bridge declaration",
        messageFormat: "Telepath view '{0}' must declare 'public override partial void _Notification(int what);'",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCreateViewModel = new(
        id: "TPV003",
        title: "Invalid CreateViewModel callback",
        messageFormat: "Telepath view '{0}' must declare one parameterless instance method returning '{1}' named CreateViewModel",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidOnBind = new(
        id: "TPV004",
        title: "Invalid OnBind callback",
        messageFormat: "Telepath view '{0}' must declare 'void OnBind({1} vm, Telepath.Godot.BindingSet bindings)' or at least one [LinkTo] member",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidOnReady = new(
        id: "TPV005",
        title: "Invalid OnReady callback",
        messageFormat: "Optional callback OnReady on Telepath view '{0}' must be a unique parameterless instance method returning void",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingMember = new(
        id: "TPV006",
        title: "Member conflicts with generated Telepath view code",
        messageFormat: "Telepath view '{0}' must not declare '{1}'; that member is generated or replaced by the Telepath lifecycle",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidViewModel = new(
        id: "TPV007",
        title: "Invalid Telepath ViewModel",
        messageFormat: "ViewModel type '{1}' on Telepath view '{0}' must implement Telepath.Core.IViewModel",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedLinkToControl = new(
        id: "TPV008",
        title: "Unsupported LinkTo control type",
        messageFormat: "[LinkTo] on '{0}.{1}' requires Godot.Label or Godot.BaseButton, but the member type is '{2}'",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidLinkTo = new(
        id: "TPV009",
        title: "Invalid LinkTo declaration",
        messageFormat: "[LinkTo] on '{0}.{1}' is invalid: {2}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
