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

        if (!TryCollectBindings(context, viewType, viewName, out var injections, out var bindings))
        {
            return;
        }

        var onBindMethods = SymbolHelpers.GetDeclaredInstanceMethods(viewType, "OnBind").ToArray();
        var hasOnBind = onBindMethods.Length == 1 && IsValidOnBind(onBindMethods[0], viewModelType);
        if (onBindMethods.Length > 0 && !hasOnBind)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidOnBind,
                candidate.Location,
                viewName,
                viewModelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }

        if (!hasOnBind && bindings.Count == 0)
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

        foreach (var conflictingName in new[]
                 {
                     "ViewModel",
                     "_Ready",
                     "_EnterTree",
                     "_ExitTree",
                     "__TelepathOnReady",
                     "__TelepathOnBind",
                 })
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
            hasOnReady: onReadyMethods.Length == 1,
            hasOnBind: hasOnBind,
            injections,
            bindings);

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

    private static bool TryCollectBindings(
        SourceProductionContext context,
        INamedTypeSymbol viewType,
        string viewName,
        out List<NodeInjection> injections,
        out List<ViewBinding> bindings)
    {
        injections = [];
        bindings = [];
        var valid = true;

        foreach (var member in viewType.GetMembers())
        {
            var injectAttributes = SymbolHelpers.GetAttributes(member, ViewMetadata.NodeInjectAttributeName).ToArray();
            var bindAttributes = SymbolHelpers.GetAttributes(member, ViewMetadata.BindToAttributeName).ToArray();
            if (injectAttributes.Length == 0 && bindAttributes.Length == 0)
            {
                continue;
            }

            ITypeSymbol? memberType = member switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol typedProperty => typedProperty.Type,
                _ => null,
            };

            var location = member.Locations.FirstOrDefault() ?? Location.None;

            if (memberType is null || member.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    injectAttributes.Length > 0 ? ViewMetadata.InvalidNodeInject : ViewMetadata.InvalidBindTo,
                    location,
                    viewName,
                    member.Name,
                    "must be an instance field or property"));
                valid = false;
                continue;
            }

            if (member is IPropertySymbol property && property.SetMethod is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    injectAttributes.Length > 0 ? ViewMetadata.InvalidNodeInject : ViewMetadata.InvalidBindTo,
                    location,
                    viewName,
                    member.Name,
                    "properties must have a setter"));
                valid = false;
                continue;
            }

            var underlyingType = memberType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            if (underlyingType is not INamedTypeSymbol namedType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.UnsupportedBindToControl,
                    location,
                    viewName,
                    member.Name,
                    memberType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                valid = false;
                continue;
            }

            if (injectAttributes.Length > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.InvalidNodeInject,
                    location,
                    viewName,
                    member.Name,
                    "only one [NodeInject] is allowed per member"));
                valid = false;
                continue;
            }

            if (bindAttributes.Length > 0 && injectAttributes.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.InvalidBindTo,
                    location,
                    viewName,
                    member.Name,
                    "requires [NodeInject] on the same member"));
                valid = false;
                continue;
            }

            string? nodePath = null;
            if (injectAttributes.Length == 1)
            {
                nodePath = injectAttributes[0].ConstructorArguments.Length > 0
                    ? injectAttributes[0].ConstructorArguments[0].Value as string
                    : null;

                if (string.IsNullOrWhiteSpace(nodePath))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ViewMetadata.InvalidNodeInject,
                        location,
                        viewName,
                        member.Name,
                        "node path must be non-empty"));
                    valid = false;
                    continue;
                }

                injections.Add(new NodeInjection(
                    member.Name,
                    nodePath!,
                    namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            foreach (var attribute in bindAttributes)
            {
                var viewModelMember = attribute.ConstructorArguments.Length > 0
                    ? attribute.ConstructorArguments[0].Value as string
                    : null;

                if (string.IsNullOrWhiteSpace(viewModelMember))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ViewMetadata.InvalidBindTo,
                        location,
                        viewName,
                        member.Name,
                        "member name must be non-empty"));
                    valid = false;
                    continue;
                }

                if (!TryResolveBindToKind(
                        context,
                        attribute,
                        namedType,
                        viewName,
                        member,
                        out var kind))
                {
                    valid = false;
                    continue;
                }

                if (!TryResolveParameter(
                        context,
                        attribute,
                        viewType,
                        namedType,
                        viewName,
                        member,
                        kind,
                        out var parameterMemberName,
                        out var parameterAccess))
                {
                    valid = false;
                    continue;
                }

                if (!TryResolveItemTemplate(
                        context,
                        attribute,
                        viewType,
                        namedType,
                        viewName,
                        member,
                        kind,
                        out var itemViewTypeDisplay,
                        out var itemViewModelTypeDisplay,
                        out var itemSceneMemberName))
                {
                    valid = false;
                    continue;
                }

                if (!TryResolveConverter(
                        context,
                        attribute,
                        viewName,
                        member,
                        kind,
                        itemViewTypeDisplay is not null,
                        out var converterTypeDisplay))
                {
                    valid = false;
                    continue;
                }

                var implicitToString = kind == BindToKind.Text
                    && converterTypeDisplay is null
                    && (SymbolHelpers.IsOrInheritsFrom(namedType, ViewMetadata.LabelName)
                        || SymbolHelpers.IsOrInheritsFrom(namedType, ViewMetadata.RichTextLabelName));

                bindings.Add(new ViewBinding(
                    member.Name,
                    viewModelMember!,
                    kind,
                    parameterMemberName,
                    parameterAccess,
                    converterTypeDisplay,
                    implicitToString,
                    itemViewTypeDisplay,
                    itemViewModelTypeDisplay,
                    itemSceneMemberName));
            }
        }

        return valid;
    }

    private static bool TryResolveBindToKind(
        SourceProductionContext context,
        AttributeData attribute,
        INamedTypeSymbol controlType,
        string viewName,
        ISymbol member,
        out BindToKind kind)
    {
        kind = default;
        var location = member.Locations.FirstOrDefault() ?? Location.None;
        var hasExplicitKind = SymbolHelpers.TryGetNamedEnum(
            attribute,
            "Kind",
            ViewMetadata.LinkKindName,
            out var rawKind);

        if (hasExplicitKind)
        {
            if (!Enum.IsDefined(typeof(BindToKind), rawKind))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.InvalidBindTo,
                    location,
                    viewName,
                    member.Name,
                    "Kind must be a Telepath.Godot.LinkKind value"));
                return false;
            }

            kind = (BindToKind)rawKind;
            if (kind == BindToKind.Auto)
            {
                hasExplicitKind = false;
            }
        }

        if (!hasExplicitKind)
        {
            if (TryInferKind(controlType, out kind))
            {
                return true;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.UnsupportedBindToControl,
                location,
                viewName,
                member.Name,
                controlType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return false;
        }

        if (IsKindCompatible(kind, controlType))
        {
            return true;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            ViewMetadata.InvalidBindTo,
            location,
            viewName,
            member.Name,
            $"Kind.{kind} is not valid for '{controlType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}'"));
        return false;
    }

    private static bool TryInferKind(INamedTypeSymbol controlType, out BindToKind kind)
    {
        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.CheckBoxName)
            || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.CheckButtonName))
        {
            kind = BindToKind.Toggle;
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.OptionButtonName))
        {
            kind = BindToKind.Selected;
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ItemListName))
        {
            kind = BindToKind.Items;
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.BaseButtonName))
        {
            kind = BindToKind.Command;
            return true;
        }

        if (IsTextControl(controlType))
        {
            kind = BindToKind.Text;
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.RangeName))
        {
            kind = BindToKind.Value;
            return true;
        }

        kind = default;
        return false;
    }

    private static bool IsKindCompatible(BindToKind kind, INamedTypeSymbol controlType)
    {
        return kind switch
        {
            BindToKind.Text => IsTextControl(controlType),
            BindToKind.Command =>
                SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.BaseButtonName)
                || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.LineEditName),
            BindToKind.Toggle or BindToKind.Disabled =>
                SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.BaseButtonName),
            BindToKind.Value => SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.RangeName),
            BindToKind.Selected =>
                SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.OptionButtonName)
                || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ItemListName),
            BindToKind.Items =>
                SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ItemListName)
                || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.OptionButtonName)
                || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ContainerName),
            BindToKind.Visible =>
                SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.CanvasItemName)
                || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ControlName),
            _ => false,
        };
    }

    private static bool IsTextControl(INamedTypeSymbol controlType)
    {
        return SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.LabelName)
            || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.RichTextLabelName)
            || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.LineEditName)
            || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.TextEditName);
    }

    private static bool TryResolveParameter(
        SourceProductionContext context,
        AttributeData attribute,
        INamedTypeSymbol viewType,
        INamedTypeSymbol targetType,
        string viewName,
        ISymbol member,
        BindToKind kind,
        out string? parameterMemberName,
        out string? parameterAccess)
    {
        parameterMemberName = null;
        parameterAccess = null;
        var parameterName = SymbolHelpers.GetNamedString(attribute, "Parameter");
        if (parameterName is not { Length: > 0 } name || string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var location = member.Locations.FirstOrDefault() ?? Location.None;
        if (kind != BindToKind.Command)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "Parameter is only valid for command bindings"));
            return false;
        }

        if (!SymbolHelpers.IsOrInheritsFrom(targetType, ViewMetadata.BaseButtonName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "Parameter requires the [BindTo] target to be a Godot.BaseButton"));
            return false;
        }

        var matches = viewType.GetMembers(name).ToArray();
        if (matches.Length != 1 || matches[0].IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                $"Parameter '{name}' must name a unique instance field or property on the view"));
            return false;
        }

        ITypeSymbol? parameterType = matches[0] switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null,
        };

        if (parameterType is not INamedTypeSymbol namedParameterType
            || !TryGetParameterAccess(namedParameterType, out parameterAccess))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                $"Parameter '{name}' must be a text, toggle, range, or option control"));
            return false;
        }

        parameterMemberName = matches[0].Name;
        return true;
    }

    private static bool TryResolveConverter(
        SourceProductionContext context,
        AttributeData attribute,
        string viewName,
        ISymbol member,
        BindToKind kind,
        bool hasItemView,
        out string? converterTypeDisplay)
    {
        converterTypeDisplay = null;
        var location = member.Locations.FirstOrDefault() ?? Location.None;
        if (!SymbolHelpers.TryGetNamedType(attribute, "Converter", out var converterType, out var specified))
        {
            if (!specified)
            {
                return true;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindToConverter,
                location,
                viewName,
                member.Name,
                "Converter must be a concrete type"));
            return false;
        }

        if (kind == BindToKind.Command || hasItemView)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindToConverter,
                location,
                viewName,
                member.Name,
                hasItemView
                    ? "Converter is not valid for container item bindings"
                    : "Converter is not valid for command bindings"));
            return false;
        }

        if (converterType is not INamedTypeSymbol namedConverter
            || namedConverter.IsAbstract
            || namedConverter.IsUnboundGenericType
            || namedConverter.TypeKind is TypeKind.Interface or TypeKind.Delegate or TypeKind.Enum
            || !SymbolHelpers.ImplementsGenericInterface(namedConverter, ViewMetadata.ValueConverterName)
            || !SymbolHelpers.HasPublicParameterlessConstructor(namedConverter))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindToConverter,
                location,
                viewName,
                member.Name,
                "Converter must be a non-abstract, non-open-generic type that implements Telepath.Core.IValueConverter<,> and has a public parameterless constructor"));
            return false;
        }

        converterTypeDisplay = namedConverter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return true;
    }

    private static bool TryResolveItemTemplate(
        SourceProductionContext context,
        AttributeData attribute,
        INamedTypeSymbol viewType,
        INamedTypeSymbol controlType,
        string viewName,
        ISymbol member,
        BindToKind kind,
        out string? itemViewTypeDisplay,
        out string? itemViewModelTypeDisplay,
        out string? itemSceneMemberName)
    {
        itemViewTypeDisplay = null;
        itemViewModelTypeDisplay = null;
        itemSceneMemberName = null;
        var location = member.Locations.FirstOrDefault() ?? Location.None;
        var hasItemView = SymbolHelpers.TryGetNamedType(attribute, "ItemView", out var itemViewType, out var itemViewSpecified);
        var itemSceneSpecified = TryGetNamedArgument(attribute, "ItemScene", out var itemSceneValue);
        var itemSceneName = itemSceneValue as string;

        if (!itemViewSpecified && !itemSceneSpecified)
        {
            if (kind == BindToKind.Items && IsContainer(controlType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ViewMetadata.InvalidBindTo,
                    location,
                    viewName,
                    member.Name,
                    "container item bindings require ItemView and ItemScene"));
                return false;
            }

            return true;
        }

        if (kind != BindToKind.Items)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "ItemView and ItemScene are only valid for Kind.Items"));
            return false;
        }

        if (!IsContainer(controlType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "ItemView and ItemScene are only valid for Godot.Container"));
            return false;
        }

        if (!itemViewSpecified || !itemSceneSpecified)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "container item bindings require both ItemView and ItemScene"));
            return false;
        }

        if (!hasItemView
            || itemViewType is not INamedTypeSymbol namedView
            || namedView.IsAbstract
            || namedView.IsUnboundGenericType
            || !SymbolHelpers.IsOrInheritsFrom(namedView, ViewMetadata.ControlName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "ItemView must be a concrete Godot.Control type"));
            return false;
        }

        if (!TryGetTelepathViewModel(namedView, out var itemViewModel))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "ItemView must be marked [TelepathView<TViewModel>]"));
            return false;
        }

        if (itemSceneName is not { Length: > 0 } sceneName || string.IsNullOrWhiteSpace(sceneName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                "ItemScene must name a PackedScene member on the view"));
            return false;
        }

        var matches = viewType.GetMembers(sceneName).ToArray();
        if (matches.Length != 1 || matches[0].IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                $"ItemScene '{sceneName}' must name a unique instance field or property on the view"));
            return false;
        }

        ITypeSymbol? sceneType = matches[0] switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => null,
        };

        var underlyingSceneType = sceneType?.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        if (underlyingSceneType is not INamedTypeSymbol namedScene
            || !SymbolHelpers.HasMetadataName(namedScene, ViewMetadata.PackedSceneName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ViewMetadata.InvalidBindTo,
                location,
                viewName,
                member.Name,
                $"ItemScene '{sceneName}' must be a Godot.PackedScene field or property"));
            return false;
        }

        itemViewTypeDisplay = namedView.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        itemViewModelTypeDisplay = itemViewModel.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        itemSceneMemberName = matches[0].Name;
        return true;
    }

    private static bool TryGetTelepathViewModel(INamedTypeSymbol viewType, out ITypeSymbol viewModelType)
    {
        if (SymbolHelpers.TryGetAttribute(viewType, ViewMetadata.ViewAttributeName, out var attribute)
            && attribute.AttributeClass?.TypeArguments.Length == 1)
        {
            viewModelType = attribute.AttributeClass.TypeArguments[0];
            return true;
        }

        viewModelType = null!;
        return false;
    }

    private static bool IsContainer(INamedTypeSymbol controlType)
        => SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.ContainerName);

    private static bool TryGetNamedArgument(AttributeData attribute, string name, out object? value)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != name)
            {
                continue;
            }

            value = argument.Value.Value;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetParameterAccess(INamedTypeSymbol controlType, out string access)
    {
        if (IsTextControl(controlType))
        {
            access = ".Text";
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.CheckBoxName)
            || SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.CheckButtonName))
        {
            access = ".ButtonPressed";
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.OptionButtonName))
        {
            access = ".Selected";
            return true;
        }

        if (SymbolHelpers.IsOrInheritsFrom(controlType, ViewMetadata.RangeName))
        {
            access = ".Value";
            return true;
        }

        access = "";
        return false;
    }
}
