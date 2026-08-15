using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Telepath.SourceGenerator;

[Generator]
public sealed class TelepathIncrementalGenerator : IIncrementalGenerator
{
    private const string ViewAttributeMetadataName = "Telepath.Godot.TelepathViewAttribute`1";
    private const string ControlMetadataName = "Godot.Control";
    private const string ViewModelMetadataName = "Telepath.Core.IViewModel";
    private const string BindingSetMetadataName = "Telepath.Godot.BindingSet";

    private static readonly DiagnosticDescriptor InvalidTarget = new(
        id: "TPV001",
        title: "Invalid Telepath view target",
        messageFormat: "Telepath view '{0}' must be a non-generic partial class derived from Godot.Control",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingNotificationBridge = new(
        id: "TPV002",
        title: "Missing Godot notification bridge declaration",
        messageFormat: "Telepath view '{0}' must declare 'public override partial void _Notification(int what);'",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidCreateViewModel = new(
        id: "TPV003",
        title: "Invalid CreateViewModel callback",
        messageFormat: "Telepath view '{0}' must declare one parameterless instance method returning '{1}' named CreateViewModel",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidOnBind = new(
        id: "TPV004",
        title: "Invalid OnBind callback",
        messageFormat: "Telepath view '{0}' must declare one instance method 'void OnBind({1} vm, Telepath.Godot.BindingSet bindings)'",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidOnReady = new(
        id: "TPV005",
        title: "Invalid OnReady callback",
        messageFormat: "Optional callback OnReady on Telepath view '{0}' must be a unique parameterless instance method returning void",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingMember = new(
        id: "TPV006",
        title: "Member conflicts with generated Telepath view code",
        messageFormat: "Telepath view '{0}' must not declare '{1}'; that member is generated or replaced by the Telepath lifecycle",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidViewModel = new(
        id: "TPV007",
        title: "Invalid Telepath ViewModel",
        messageFormat: "ViewModel type '{1}' on Telepath view '{0}' must implement Telepath.Core.IViewModel",
        category: "Telepath.View",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var views = context.SyntaxProvider.ForAttributeWithMetadataName(
            ViewAttributeMetadataName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, _) => CreateCandidate(attributeContext));

        context.RegisterSourceOutput(views, static (productionContext, candidate) =>
        {
            GenerateView(productionContext, candidate);
        });
    }

    private static ViewCandidate CreateCandidate(GeneratorAttributeSyntaxContext context)
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

    private static void GenerateView(SourceProductionContext context, ViewCandidate candidate)
    {
        var viewType = candidate.ViewType;
        var viewModelType = candidate.ViewModelType;
        var viewName = viewType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (viewModelType is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidViewModel,
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
            || !IsOrInheritsFrom(viewType, ControlMetadataName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidTarget,
                candidate.Location,
                viewName));
            return;
        }

        if (!ImplementsInterface(viewModelType, ViewModelMetadataName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidViewModel,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        if (!HasNotificationBridgeDeclaration(viewType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingNotificationBridge,
                candidate.Location,
                viewName));
            return;
        }

        var viewModelDisplay = viewModelType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var createViewModelMethods = GetDeclaredInstanceMethods(viewType, "CreateViewModel").ToArray();
        if (createViewModelMethods.Length != 1
            || createViewModelMethods[0].Parameters.Length != 0
            || !SymbolEqualityComparer.Default.Equals(createViewModelMethods[0].ReturnType, viewModelType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidCreateViewModel,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        var onBindMethods = GetDeclaredInstanceMethods(viewType, "OnBind").ToArray();
        if (onBindMethods.Length != 1 || !IsValidOnBind(onBindMethods[0], viewModelType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidOnBind,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        var onReadyMethods = GetDeclaredInstanceMethods(viewType, "OnReady").ToArray();
        if (onReadyMethods.Length > 1
            || onReadyMethods.Length == 1
            && (onReadyMethods[0].Parameters.Length != 0
                || !onReadyMethods[0].ReturnsVoid))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidOnReady,
                candidate.Location,
                viewName));
            return;
        }

        foreach (var conflictingName in new[] { "ViewModel", "_Ready", "_EnterTree", "_ExitTree" })
        {
            if (viewType.GetMembers(conflictingName).Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ConflictingMember,
                    candidate.Location,
                    viewName,
                    conflictingName));
                return;
            }
        }

        var source = RenderSource(
            viewType,
            viewModelDisplay,
            hasOnReady: onReadyMethods.Length == 1);

        context.AddSource(
            $"{SanitizeHintName(viewType.ToDisplayString())}.TelepathView.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static bool HasNotificationBridgeDeclaration(INamedTypeSymbol viewType)
    {
        var methods = GetDeclaredInstanceMethods(viewType, "_Notification").ToArray();
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

    private static IEnumerable<IMethodSymbol> GetDeclaredInstanceMethods(
        INamedTypeSymbol type,
        string name)
    {
        return type.GetMembers(name)
            .OfType<IMethodSymbol>()
            .Where(static method =>
                !method.IsStatic
                && method.MethodKind == MethodKind.Ordinary);
    }

    private static bool IsValidOnBind(IMethodSymbol method, ITypeSymbol viewModelType)
    {
        return method.ReturnsVoid
            && method.Parameters.Length == 2
            && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, viewModelType)
            && HasMetadataName(method.Parameters[1].Type, BindingSetMetadataName);
    }

    private static bool ImplementsInterface(ITypeSymbol type, string metadataName)
    {
        return type is INamedTypeSymbol namedType
            && namedType.AllInterfaces.Any(interfaceType => HasMetadataName(interfaceType, metadataName));
    }

    private static bool IsOrInheritsFrom(INamedTypeSymbol type, string metadataName)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (HasMetadataName(current, metadataName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMetadataName(ITypeSymbol type, string metadataName)
    {
        var separator = metadataName.LastIndexOf('.');
        return type.MetadataName == metadataName.Substring(separator + 1)
            && type.ContainingNamespace.ToDisplayString()
                == metadataName.Substring(0, separator);
    }

    private static string RenderSource(
        INamedTypeSymbol viewType,
        string viewModelDisplay,
        bool hasOnReady)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");

        if (!viewType.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append("namespace ")
                .Append(viewType.ContainingNamespace.ToDisplayString())
                .AppendLine(";");
            source.AppendLine();
        }

        AppendContainingTypesStart(source, viewType.ContainingType);

        source.Append("partial class @")
            .Append(viewType.Name)
            .AppendLine();
        source.AppendLine("{");
        source.Append("    private global::Telepath.Godot.ViewLifecycle<")
            .Append(viewModelDisplay)
            .AppendLine(">? __telepathViewLifecycle;");
        source.AppendLine();
        source.Append("    private global::Telepath.Godot.ViewLifecycle<")
            .Append(viewModelDisplay)
            .AppendLine("> __TelepathViewLifecycle");
        source.AppendLine("        => __telepathViewLifecycle ??= new(");
        source.AppendLine("            this,");
        source.Append("            ")
            .Append(hasOnReady ? "OnReady" : "static () => { }")
            .AppendLine(",");
        source.AppendLine("            CreateViewModel,");
        source.AppendLine("            OnBind);");
        source.AppendLine();
        source.Append("    public ")
            .Append(viewModelDisplay)
            .AppendLine("? ViewModel");
        source.AppendLine("    {");
        source.AppendLine("        get => __TelepathViewLifecycle.ViewModel;");
        source.AppendLine("        set => __TelepathViewLifecycle.ViewModel = value;");
        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    public override partial void _Notification(int what)");
        source.AppendLine("    {");
        source.AppendLine("        base._Notification(what);");
        source.AppendLine("        __TelepathViewLifecycle.HandleNotification(what);");
        source.AppendLine("    }");
        source.AppendLine("}");

        AppendContainingTypesEnd(source, viewType.ContainingType);
        return source.ToString();
    }

    private static void AppendContainingTypesStart(
        StringBuilder source,
        INamedTypeSymbol? containingType)
    {
        if (containingType is null)
        {
            return;
        }

        AppendContainingTypesStart(source, containingType.ContainingType);
        source.Append("partial ")
            .Append(containingType.TypeKind == TypeKind.Struct ? "struct @" : "class @")
            .Append(containingType.Name)
            .AppendLine();
        source.AppendLine("{");
    }

    private static void AppendContainingTypesEnd(
        StringBuilder source,
        INamedTypeSymbol? containingType)
    {
        while (containingType is not null)
        {
            source.AppendLine("}");
            containingType = containingType.ContainingType;
        }
    }

    private static string SanitizeHintName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private sealed class ViewCandidate
    {
        public ViewCandidate(
            INamedTypeSymbol viewType,
            ITypeSymbol? viewModelType,
            Location location)
        {
            ViewType = viewType;
            ViewModelType = viewModelType;
            Location = location;
        }

        public INamedTypeSymbol ViewType { get; }

        public ITypeSymbol? ViewModelType { get; }

        public Location Location { get; }
    }
}
