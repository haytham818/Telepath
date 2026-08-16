using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Telepath.SourceGenerator;

internal static class ViewModelGenerator
{
    public static ViewModelMemberCandidate CreateBindableMember(GeneratorAttributeSyntaxContext context)
    {
        var attribute = context.Attributes[0];
        var from = ReadFromArguments(attribute);
        var explicitName = SymbolHelpers.GetNamedString(attribute, "Name");
        var location = context.TargetNode.GetLocation();

        if (context.TargetSymbol is IFieldSymbol field)
        {
            return new ViewModelMemberCandidate(
                field.ContainingType,
                field,
                ViewModelMemberKind.BindableField,
                from,
                canExecute: null,
                explicitName,
                location);
        }

        if (context.TargetSymbol is IMethodSymbol method)
        {
            return new ViewModelMemberCandidate(
                method.ContainingType,
                method,
                ViewModelMemberKind.BindableMethod,
                from,
                canExecute: null,
                explicitName,
                location);
        }

        return new ViewModelMemberCandidate(
            context.TargetSymbol.ContainingType,
            context.TargetSymbol,
            ViewModelMemberKind.BindableField,
            from,
            canExecute: null,
            explicitName,
            location);
    }

    public static ViewModelMemberCandidate CreateCommandMember(GeneratorAttributeSyntaxContext context)
    {
        var attribute = context.Attributes[0];
        return new ViewModelMemberCandidate(
            context.TargetSymbol.ContainingType,
            context.TargetSymbol,
            ViewModelMemberKind.Command,
            ImmutableArray<string>.Empty,
            SymbolHelpers.GetNamedString(attribute, "CanExecute"),
            SymbolHelpers.GetNamedString(attribute, "Name"),
            context.TargetNode.GetLocation());
    }

    public static void Generate(
        SourceProductionContext context,
        ImmutableArray<ViewModelMemberCandidate> bindables,
        ImmutableArray<ViewModelMemberCandidate> commands)
    {
        var groups = bindables.Concat(commands)
            .Where(static candidate => candidate.ContainingType is not null)
            .GroupBy(static candidate => candidate.ContainingType, SymbolEqualityComparer.Default);

        foreach (var group in groups)
        {
            GenerateType(context, (INamedTypeSymbol)group.Key!, group.ToArray());
        }
    }

    private static void GenerateType(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ViewModelMemberCandidate[] members)
    {
        var typeName = viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var location = members[0].Location;

        var isPartial = viewModelType.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .All(static declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial
            || viewModelType.Arity != 0
            || viewModelType.ContainingType is not null
            || !SymbolHelpers.IsOrInheritsFrom(viewModelType, ViewModelMetadata.ViewModelName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidTarget,
                location,
                typeName));
            return;
        }

        var generatedNames = new HashSet<string>(StringComparer.Ordinal);
        var plannedBindableNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in members)
        {
            if (TryPeekBindableName(candidate, out var plannedName))
            {
                plannedBindableNames.Add(plannedName);
            }
        }

        var models = new List<ViewModelGeneratedMember>();

        foreach (var candidate in members.OrderBy(static member => member.Kind))
        {
            if (!TryCreateGeneratedMember(
                    context,
                    viewModelType,
                    candidate,
                    generatedNames,
                    plannedBindableNames,
                    out var model))
            {
                continue;
            }

            generatedNames.Add(model.PropertyName);
            models.Add(model);
        }

        if (models.Count == 0)
        {
            return;
        }

        var source = ViewModelSourceRenderer.Render(viewModelType, models);
        context.AddSource(
            $"{SymbolHelpers.SanitizeHintName(viewModelType.ToDisplayString())}.TelepathViewModel.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static ImmutableArray<string> ReadFromArguments(AttributeData attribute)
    {
        var fromSemantic = SymbolHelpers.GetConstructorStringArray(attribute);
        if (fromSemantic.Length > 0)
        {
            return fromSemantic;
        }

        if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax syntax
            || syntax.ArgumentList is null)
        {
            return ImmutableArray<string>.Empty;
        }

        var names = new List<string>();
        foreach (var argument in syntax.ArgumentList.Arguments)
        {
            if (argument.NameEquals is not null)
            {
                continue;
            }

            if (argument.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
                && invocation.ArgumentList.Arguments.Count == 1)
            {
                names.Add(GetNameofIdentifier(invocation.ArgumentList.Arguments[0].Expression));
                continue;
            }

            if (argument.Expression is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                names.Add(literal.Token.ValueText);
            }
        }

        return names.ToImmutableArray();
    }

    private static string GetNameofIdentifier(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => expression.ToString(),
        };
    }

    private static bool TryPeekBindableName(ViewModelMemberCandidate candidate, out string name)
    {
        name = candidate.Kind switch
        {
            ViewModelMemberKind.BindableField when candidate.Member is IFieldSymbol field
                => GeneratedMemberNames.FromField(field.Name, candidate.ExplicitName),
            ViewModelMemberKind.BindableMethod when candidate.Member is IMethodSymbol method
                => GeneratedMemberNames.FromBindableMethod(method.Name, candidate.ExplicitName),
            _ => string.Empty,
        };

        return name.Length > 0;
    }

    private static bool TryCreateGeneratedMember(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ViewModelMemberCandidate candidate,
        HashSet<string> generatedNames,
        HashSet<string> plannedBindableNames,
        out ViewModelGeneratedMember model)
    {
        model = null!;

        switch (candidate.Kind)
        {
            case ViewModelMemberKind.BindableField:
                return TryCreateBindableField(context, viewModelType, candidate, generatedNames, out model);
            case ViewModelMemberKind.BindableMethod:
                return TryCreateBindableMethod(
                    context,
                    viewModelType,
                    candidate,
                    generatedNames,
                    plannedBindableNames,
                    out model);
            case ViewModelMemberKind.Command:
                return TryCreateCommand(context, viewModelType, candidate, generatedNames, out model);
            default:
                return false;
        }
    }

    private static bool TryCreateBindableField(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ViewModelMemberCandidate candidate,
        HashSet<string> generatedNames,
        out ViewModelGeneratedMember model)
    {
        model = null!;
        if (candidate.Member is not IFieldSymbol field
            || field.IsStatic
            || field.IsConst
            || field.IsFixedSizeBuffer)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                candidate.Member?.Name ?? "<unknown>",
                "must be a non-static instance field"));
            return false;
        }

        if (candidate.From.Length > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                field.Name,
                "source fields cannot specify From"));
            return false;
        }

        var isObservableList = IsObservableList(field.Type);
        if (isObservableList && field.IsReadOnly)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                field.Name,
                "ObservableList bindables cannot be readonly"));
            return false;
        }

        var propertyName = GeneratedMemberNames.FromField(field.Name, candidate.ExplicitName);
        if (!ValidateGeneratedName(context, viewModelType, field, propertyName, generatedNames, candidate.Location))
        {
            return false;
        }

        if (isObservableList)
        {
            var listType = ((INamedTypeSymbol)field.Type)
                .WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            model = new ViewModelGeneratedMember(
                ViewModelMemberKind.BindableListField,
                propertyName,
                field.Name,
                listType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ImmutableArray<string>.Empty,
                canExecute: null,
                canExecuteIsMethod: false);
            return true;
        }

        model = new ViewModelGeneratedMember(
            ViewModelMemberKind.BindableField,
            propertyName,
            field.Name,
            field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ImmutableArray<string>.Empty,
            canExecute: null,
            canExecuteIsMethod: false);
        return true;
    }

    private static bool IsObservableList(ITypeSymbol type)
    {
        return type is INamedTypeSymbol named
            && named.OriginalDefinition is INamedTypeSymbol original
            && original.Arity == 1
            && SymbolHelpers.HasMetadataName(original, ViewModelMetadata.ObservableListName);
    }

    private static bool TryCreateBindableMethod(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ViewModelMemberCandidate candidate,
        HashSet<string> generatedNames,
        HashSet<string> plannedBindableNames,
        out ViewModelGeneratedMember model)
    {
        model = null!;
        if (candidate.Member is not IMethodSymbol method
            || method.MethodKind != MethodKind.Ordinary
            || method.ReturnsVoid
            || method.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                candidate.Member?.Name ?? "<unknown>",
                "must be a non-generic method with a return value"));
            return false;
        }

        if (candidate.From.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                method.Name,
                "derived bindables must specify at least one From source"));
            return false;
        }

        if (candidate.From.Length != method.Parameters.Length)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidBindable,
                candidate.Location,
                method.Name,
                "From count must match the method parameter count"));
            return false;
        }

        var knownNames = new HashSet<string>(plannedBindableNames, StringComparer.Ordinal);
        foreach (var other in viewModelType.GetMembers())
        {
            if (other is IPropertySymbol or IFieldSymbol)
            {
                knownNames.Add(other.Name);
            }
        }

        foreach (var from in candidate.From)
        {
            if (string.IsNullOrEmpty(from) || !knownNames.Contains(from))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewModelMetadata.FromMemberNotFound,
                    candidate.Location,
                    method.Name,
                    from));
                return false;
            }
        }

        var propertyName = GeneratedMemberNames.FromBindableMethod(method.Name, candidate.ExplicitName);
        if (!ValidateGeneratedName(context, viewModelType, method, propertyName, generatedNames, candidate.Location))
        {
            return false;
        }

        model = new ViewModelGeneratedMember(
            ViewModelMemberKind.BindableMethod,
            propertyName,
            method.Name,
            method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            candidate.From,
            canExecute: null,
            canExecuteIsMethod: false);
        return true;
    }

    private static bool TryCreateCommand(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ViewModelMemberCandidate candidate,
        HashSet<string> generatedNames,
        out ViewModelGeneratedMember model)
    {
        model = null!;
        if (candidate.Member is not IMethodSymbol method
            || method.IsStatic
            || method.MethodKind != MethodKind.Ordinary
            || !method.ReturnsVoid
            || method.IsGenericMethod
            || method.Parameters.Length > 1
            || method.Parameters.Any(static parameter => parameter.RefKind != RefKind.None))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.InvalidCommand,
                candidate.Location,
                candidate.Member?.Name ?? "<unknown>",
                "must be an instance method returning void with zero or one parameter"));
            return false;
        }

        var canExecuteIsMethod = false;
        if (candidate.CanExecute is { Length: > 0 } canExecuteName)
        {
            if (!TryResolveCanExecute(viewModelType, canExecuteName, out canExecuteIsMethod))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewModelMetadata.CanExecuteNotFound,
                    candidate.Location,
                    method.Name,
                    canExecuteName));
                return false;
            }
        }

        var propertyName = GeneratedMemberNames.FromCommandMethod(method.Name, candidate.ExplicitName);
        if (!ValidateGeneratedName(context, viewModelType, method, propertyName, generatedNames, candidate.Location))
        {
            return false;
        }

        var parameterTypeDisplay = method.Parameters.Length == 1
            ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : null;
        var commandTypeDisplay = parameterTypeDisplay is null
            ? "global::R3.ReactiveCommand"
            : $"global::R3.ReactiveCommand<{parameterTypeDisplay}>";

        model = new ViewModelGeneratedMember(
            ViewModelMemberKind.Command,
            propertyName,
            method.Name,
            commandTypeDisplay,
            ImmutableArray<string>.Empty,
            candidate.CanExecute,
            canExecuteIsMethod,
            parameterTypeDisplay);
        return true;
    }

    private static bool TryResolveCanExecute(
        INamedTypeSymbol viewModelType,
        string name,
        out bool isMethod)
    {
        isMethod = false;
        var matches = viewModelType.GetMembers(name).ToArray();
        if (matches.Length != 1)
        {
            return false;
        }

        switch (matches[0])
        {
            case IPropertySymbol property
                when !property.IsStatic && SymbolHelpers.IsObservableOfBool(property.Type):
                return true;
            case IMethodSymbol method
                when !method.IsStatic
                    && method.Parameters.Length == 0
                    && SymbolHelpers.IsObservableOfBool(method.ReturnType):
                isMethod = true;
                return true;
            default:
                return false;
        }
    }

    private static bool ValidateGeneratedName(
        SourceProductionContext context,
        INamedTypeSymbol viewModelType,
        ISymbol sourceMember,
        string propertyName,
        HashSet<string> generatedNames,
        Location location)
    {
        if (generatedNames.Contains(propertyName)
            || propertyName == sourceMember.Name
            || viewModelType.GetMembers(propertyName).Any()
            || viewModelType.GetMembers(GeneratedMemberNames.BackingField(propertyName)).Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewModelMetadata.ConflictingMember,
                location,
                propertyName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return false;
        }

        return true;
    }
}
