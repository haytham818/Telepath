using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Telepath.SourceGenerator;

internal static class ViewGenerator
{
    public static ViewCandidate CreateCandidate(GeneratorAttributeSyntaxContext context)
    {
        var viewType = (INamedTypeSymbol)context.TargetSymbol;
        var attribute = context.Attributes[0];
        var viewModelType = attribute.AttributeClass?.TypeArguments.Length == 1
            ? attribute.AttributeClass.TypeArguments[0]
            : null;

        return new ViewCandidate(
            viewType,
            viewModelType,
            context.TargetNode.GetLocation());
    }

    public static void Generate(SourceProductionContext context, ViewCandidate candidate)
    {
        var viewType = candidate.ViewType;
        var viewModelType = candidate.ViewModelType;
        var viewName = viewType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (viewModelType is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidViewModel,
                candidate.Location,
                viewName,
                "<unknown>"));
            return;
        }

        var isPartial = viewType.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial
            || viewType.Arity != 0
            || viewType.ContainingType is not null
            || !SymbolHelpers.IsOrInheritsFrom(viewType, ViewMetadata.ControlName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidTarget,
                candidate.Location,
                viewName));
            return;
        }

        if (!SymbolHelpers.ImplementsInterface(viewModelType, ViewMetadata.ViewModelName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidViewModel,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        if (!HasNotificationBridgeDeclaration(viewType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.MissingNotificationBridge,
                candidate.Location,
                viewName));
            return;
        }

        var viewModelDisplay = viewModelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var createViewModelMethods = SymbolHelpers.GetDeclaredInstanceMethods(viewType, "CreateViewModel").ToArray();
        if (createViewModelMethods.Length != 1
            || createViewModelMethods[0].Parameters.Length != 0
            || !SymbolEqualityComparer.Default.Equals(createViewModelMethods[0].ReturnType, viewModelType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidCreateViewModel,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        var onBindMethods = SymbolHelpers.GetDeclaredInstanceMethods(viewType, "OnBind").ToArray();
        if (onBindMethods.Length != 1 || !IsValidOnBind(onBindMethods[0], viewModelType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidOnBind,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        var onReadyMethods = SymbolHelpers.GetDeclaredInstanceMethods(viewType, "OnReady").ToArray();
        if (onReadyMethods.Length > 1
            || onReadyMethods.Length == 1
            && (onReadyMethods[0].Parameters.Length != 0
                || !onReadyMethods[0].ReturnsVoid))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidOnReady,
                candidate.Location,
                viewName));
            return;
        }

        foreach (var conflictingName in new[] { "ViewModel", "_Ready", "_EnterTree", "_ExitTree" })
        {
            if (viewType.GetMembers(conflictingName).Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.ConflictingMember,
                    candidate.Location,
                    viewName,
                    conflictingName));
                return;
            }
        }

        var source = ViewSourceRenderer.Render(
            viewType,
            viewModelDisplay,
            hasOnReady: onReadyMethods.Length == 1);

        context.AddSource(
            $"{SymbolHelpers.SanitizeHintName(viewType.ToDisplayString())}.TelepathView.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static bool HasNotificationBridgeDeclaration(INamedTypeSymbol viewType)
    {
        var methods = SymbolHelpers.GetDeclaredInstanceMethods(viewType, "_Notification").ToArray();
        if (methods.Length != 1)
        {
            return false;
        }

        var method = methods[0];
        if (!method.ReturnsVoid
            || method.DeclaredAccessibility != Accessibility.Public
            || !method.IsOverride
            || method.Parameters.Length != 1
            || method.Parameters[0].Type.SpecialType != SpecialType.System_Int32)
        {
            return false;
        }

        return method.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<MethodDeclarationSyntax>()
            .Any(static declaration =>
                declaration.Modifiers.Any(SyntaxKind.PartialKeyword)
                && declaration.Body is null
                && declaration.ExpressionBody is null
                && !declaration.SemicolonToken.IsKind(SyntaxKind.None));
    }

    private static bool IsValidOnBind(IMethodSymbol method, ITypeSymbol viewModelType)
    {
        return method.ReturnsVoid
            && method.Parameters.Length == 2
            && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, viewModelType)
            && SymbolHelpers.HasMetadataName(method.Parameters[1].Type, ViewMetadata.BindingSetName);
    }
}
