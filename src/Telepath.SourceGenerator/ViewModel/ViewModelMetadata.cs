using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal static class ViewModelMetadata
{
    public const string BindableAttributeName = "Telepath.Core.BindableAttribute";
    public const string CommandAttributeName = "Telepath.Core.CommandAttribute";
    public const string ViewModelName = "Telepath.Core.ViewModel";
    public const string DiagnosticCategory = "Telepath.ViewModel";

    public static readonly DiagnosticDescriptor InvalidTarget = new(
        id: "TPM001",
        title: "Invalid Telepath ViewModel target",
        messageFormat: "Telepath ViewModel '{0}' must be a non-generic partial class derived from Telepath.Core.ViewModel",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidBindable = new(
        id: "TPM002",
        title: "Invalid [Bindable] target",
        messageFormat: "[Bindable] on '{0}' is invalid: {1}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FromMemberNotFound = new(
        id: "TPM003",
        title: "Bindable From member not found",
        messageFormat: "[Bindable] on '{0}' references unknown source '{1}'",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCommand = new(
        id: "TPM004",
        title: "Invalid [Command] target",
        messageFormat: "[Command] on '{0}' is invalid: {1}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CanExecuteNotFound = new(
        id: "TPM005",
        title: "Command CanExecute member not found",
        messageFormat: "[Command] on '{0}' references CanExecute '{1}', which must be an Observable<bool> property or parameterless method",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingMember = new(
        id: "TPM006",
        title: "Generated ViewModel member conflict",
        messageFormat: "Generated member '{0}' on '{1}' conflicts with an existing or generated member",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
