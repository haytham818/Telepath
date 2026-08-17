using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal static class ViewMetadata
{
    public const string ViewAttributeName = "Telepath.Godot.TelepathViewAttribute`1";
    public const string NodeInjectAttributeName = "Telepath.Godot.NodeInjectAttribute";
    public const string BindToAttributeName = "Telepath.Godot.BindToAttribute";
    public const string ControlName = "Godot.Control";
    public const string CanvasItemName = "Godot.CanvasItem";
    public const string LabelName = "Godot.Label";
    public const string RichTextLabelName = "Godot.RichTextLabel";
    public const string LineEditName = "Godot.LineEdit";
    public const string TextEditName = "Godot.TextEdit";
    public const string BaseButtonName = "Godot.BaseButton";
    public const string CheckBoxName = "Godot.CheckBox";
    public const string CheckButtonName = "Godot.CheckButton";
    public const string OptionButtonName = "Godot.OptionButton";
    public const string ItemListName = "Godot.ItemList";
    public const string ContainerName = "Godot.Container";
    public const string PackedSceneName = "Godot.PackedScene";
    public const string RangeName = "Godot.Range";
    public const string ViewModelName = "Telepath.Core.IViewModel";
    public const string TelepathViewInterfaceName = "Telepath.Godot.ITelepathView`1";
    public const string BindingSetName = "Telepath.Core.BindingSet";
    public const string ValueConverterName = "Telepath.Core.IValueConverter`2";
    public const string LinkKindName = "Telepath.Godot.LinkKind";
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
        messageFormat: "OnBind on Telepath view '{0}' must be 'void OnBind({1} vm, Telepath.Core.BindingSet bindings)'",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidOnReady = new(
        id: "TPV005",
        title: "Invalid view lifecycle callback",
        messageFormat: "Optional callback '{1}' on Telepath view '{0}' must be a unique parameterless instance method returning void",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidOnUnbind = new(
        id: "TPV012",
        title: "Invalid OnUnbind callback",
        messageFormat: "OnUnbind on Telepath view '{0}' must be 'void OnUnbind({1} vm)'",
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

    public static readonly DiagnosticDescriptor UnsupportedBindToControl = new(
        id: "TPV008",
        title: "Unsupported BindTo control type",
        messageFormat: "[BindTo] on '{0}.{1}' cannot infer a binding for '{2}'; use a supported control or set Kind",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidNodeInject = new(
        id: "TPV009",
        title: "Invalid NodeInject declaration",
        messageFormat: "[NodeInject] on '{0}.{1}' is invalid: {2}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidBindTo = new(
        id: "TPV010",
        title: "Invalid BindTo declaration",
        messageFormat: "[BindTo] on '{0}.{1}' is invalid: {2}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidBindToConverter = new(
        id: "TPV011",
        title: "Invalid BindTo converter",
        messageFormat: "[BindTo] on '{0}.{1}' has an invalid Converter: {2}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
