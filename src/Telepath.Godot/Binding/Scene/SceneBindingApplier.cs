using System.Reflection;
using Godot;
using ObservableCollections;
using R3;
using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Applies <see cref="SceneBindingSchema"/> entries onto a <see cref="BindingSet"/>
/// using the same Target / command adapters as generated <c>[BindTo]</c> code.
/// </summary>
public static class SceneBindingApplier
{
    public static void Apply(Control view, object viewModel, BindingSet bindings)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(bindings);

        foreach (var entry in SceneBindingSchema.Read(view))
        {
            ApplyEntry(view, viewModel, bindings, entry);
        }
    }

    private static void ApplyEntry(
        Control view,
        object viewModel,
        BindingSet bindings,
        SceneBindingEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Path))
        {
            throw new InvalidOperationException("Scene binding is missing a node path.");
        }

        if (string.IsNullOrWhiteSpace(entry.Member))
        {
            throw new InvalidOperationException($"Scene binding '{entry.Path}' is missing a ViewModel member.");
        }

        var node = view.GetNode(entry.Path);
        var kind = LinkKindInference.Resolve(entry.Kind, node);
        var source = GetMemberValue(viewModel, entry.Member, allowNull: kind == LinkKind.View);

        switch (kind)
        {
            case LinkKind.Command:
                ApplyCommand(bindings, source!, node, view, entry.Parameter);
                break;
            case LinkKind.Items:
                ApplyItems(bindings, source!, node, entry);
                break;
            case LinkKind.View:
                ApplyView(bindings, source, node, entry.Converter);
                break;
            default:
                ApplyValue(bindings, source!, node, kind, entry.Converter);
                break;
        }
    }

    private static object? GetMemberValue(object viewModel, string member, bool allowNull)
    {
        var type = viewModel.GetType();
        var property = type.GetProperty(member, BindingFlags.Instance | BindingFlags.Public);
        if (property is null)
        {
            throw new InvalidOperationException(
                $"ViewModel '{type.Name}' has no public property '{member}'.");
        }

        var value = property.GetValue(viewModel);
        if (value is null && !allowNull)
        {
            throw new InvalidOperationException(
                $"ViewModel '{type.Name}.{member}' is null.");
        }

        return value;
    }

    private static void ApplyValue(
        BindingSet bindings,
        object source,
        Node node,
        LinkKind kind,
        string? converterTypeName)
    {
        var target = CreateTarget(node, kind);
        var converter = CreateConverter(converterTypeName);
        var implicitToString = converter is null && LinkKindInference.IsLabelText(node);
        RuntimeBind.Bind(bindings, source, target, converter, implicitToString);
    }

    private static object CreateTarget(Node node, LinkKind kind)
        => kind switch
        {
            LinkKind.Text => node switch
            {
                Label label => label.Text(),
                RichTextLabel rich => rich.Text(),
                LineEdit edit => edit.Text(),
                TextEdit edit => edit.Text(),
                _ => throw Incompatible(kind, node),
            },
            LinkKind.Toggle => node is BaseButton button
                ? button.Toggle()
                : throw Incompatible(kind, node),
            LinkKind.Value => node is global::Godot.Range range
                ? range.Value()
                : throw Incompatible(kind, node),
            LinkKind.Selected => node switch
            {
                OptionButton option => option.Selected(),
                ItemList list => list.Selected(),
                _ => throw Incompatible(kind, node),
            },
            LinkKind.Visible => node is CanvasItem canvas
                ? canvas.Visible()
                : throw Incompatible(kind, node),
            LinkKind.Disabled => node is BaseButton disabled
                ? disabled.Disabled()
                : throw Incompatible(kind, node),
            _ => throw new InvalidOperationException($"Unsupported value binding kind '{kind}'."),
        };

    private static void ApplyCommand(
        BindingSet bindings,
        object source,
        Node node,
        Control view,
        string? parameterPath)
    {
        var classified = SourceClassifier.Classify(source);
        Node? parameterNode = null;
        if (!string.IsNullOrWhiteSpace(parameterPath))
        {
            parameterNode = view.GetNode(parameterPath);
        }

        if (classified.Kind == SourceKind.Command)
        {
            if (node is not BaseButton button)
            {
                throw Incompatible(LinkKind.Command, node);
            }

            bindings.BindCommand((ReactiveCommand)source, button);
            return;
        }

        if (classified.Kind != SourceKind.CommandT)
        {
            throw new InvalidOperationException(
                $"Command binding requires a ReactiveCommand, got '{source.GetType().Name}'.");
        }

        if (node is LineEdit edit && parameterNode is null && classified.ValueType == typeof(string))
        {
            bindings.BindCommand((ReactiveCommand<string>)source, edit);
            return;
        }

        if (node is OptionButton option && parameterNode is null && classified.ValueType == typeof(long))
        {
            bindings.BindCommand((ReactiveCommand<long>)source, option);
            return;
        }

        if (node is not BaseButton commandButton)
        {
            throw Incompatible(LinkKind.Command, node);
        }

        if (parameterNode is null)
        {
            throw new InvalidOperationException(
                $"ReactiveCommand<{classified.ValueType.Name}> on '{commandButton.Name}' requires a Parameter control.");
        }

        RuntimeBind.BindCommand(bindings, source, commandButton, parameterNode, classified.ValueType);
    }

    private static void ApplyItems(
        BindingSet bindings,
        object source,
        Node node,
        SceneBindingEntry entry)
    {
        var classified = SourceClassifier.Classify(source);
        var converter = CreateConverter(entry.Converter);

        if (node is ItemList list)
        {
            RuntimeBind.BindItems(bindings, source, classified, list.Items(), converter);
            return;
        }

        if (node is OptionButton option)
        {
            RuntimeBind.BindItems(bindings, source, classified, option.Items(), converter);
            return;
        }

        if (node is not Container container)
        {
            throw Incompatible(LinkKind.Items, node);
        }

        if (converter is not null)
        {
            throw new InvalidOperationException("Converter is not valid for container item bindings.");
        }

        if (string.IsNullOrWhiteSpace(entry.ItemScene))
        {
            throw new InvalidOperationException(
                $"Container '{container.Name}' item binding requires item_scene.");
        }

        var scene = GD.Load<PackedScene>(entry.ItemScene)
            ?? throw new InvalidOperationException($"Failed to load item scene '{entry.ItemScene}'.");
        RuntimeBind.BindContainerItems(bindings, source, classified, container, scene);
    }

    private static void ApplyView(
        BindingSet bindings,
        object? source,
        Node node,
        string? converterTypeName)
    {
        if (!string.IsNullOrWhiteSpace(converterTypeName))
        {
            throw new InvalidOperationException("Converter is not valid for View bindings.");
        }

        if (node is not Control control)
        {
            throw Incompatible(LinkKind.View, node);
        }

        var target = control.View();
        if (source is null)
        {
            bindings.BindView((IViewModel?)null, target);
            return;
        }

        if (source is IViewModel viewModel)
        {
            bindings.BindView(viewModel, target);
            return;
        }

        var classified = SourceClassifier.Classify(source);
        if (classified.Kind is not (SourceKind.Bindable or SourceKind.Observable)
            || !typeof(IViewModel).IsAssignableFrom(classified.ValueType))
        {
            throw new InvalidOperationException(
                $"View binding requires IViewModel or Observable<IViewModel>, got '{source.GetType().Name}'.");
        }

        typeof(ViewBindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method =>
                method.Name == nameof(ViewBindingExtensions.BindView)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 3)
            .MakeGenericMethod(classified.ValueType)
            .Invoke(null, [bindings, source, target]);
    }

    private static object? CreateConverter(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        var type = TypeResolver.Resolve(typeName);
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Could not construct converter '{typeName}'.");
    }

    private static InvalidOperationException Incompatible(LinkKind kind, Node node)
        => new($"Kind.{kind} is not valid for '{node.GetType().Name}'.");
}

internal enum SourceKind
{
    Bindable,
    Observable,
    Command,
    CommandT,
    List,
}

internal readonly record struct ClassifiedSource(SourceKind Kind, Type ValueType);

internal static class SourceClassifier
{
    public static ClassifiedSource Classify(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var type = source.GetType();
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current == typeof(ReactiveCommand))
            {
                return new ClassifiedSource(SourceKind.Command, typeof(Unit));
            }

            if (!current.IsGenericType)
            {
                continue;
            }

            var definition = current.GetGenericTypeDefinition();
            if (definition == typeof(BindableReactiveProperty<>))
            {
                return new ClassifiedSource(SourceKind.Bindable, current.GetGenericArguments()[0]);
            }

            if (definition == typeof(ReactiveCommand<>))
            {
                return new ClassifiedSource(SourceKind.CommandT, current.GetGenericArguments()[0]);
            }

            if (definition == typeof(ObservableList<>))
            {
                return new ClassifiedSource(SourceKind.List, current.GetGenericArguments()[0]);
            }
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Observable<>))
            {
                return new ClassifiedSource(SourceKind.Observable, current.GetGenericArguments()[0]);
            }
        }

        throw new InvalidOperationException(
            $"'{type.Name}' is not a bindable ViewModel member.");
    }
}

internal static class TypeResolver
{
    public static Type Resolve(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var type = Type.GetType(name, throwOnError: false);
        if (type is not null)
        {
            return type;
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            type = assembly.GetType(name, throwOnError: false, ignoreCase: false);
            if (type is not null)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"Could not resolve type '{name}'.");
    }
}

internal static class RuntimeBind
{
    public static void Bind(
        BindingSet bindings,
        object source,
        object target,
        object? converter,
        bool implicitToString)
    {
        var targetType = target.GetType();
        if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(BindingTarget<>))
        {
            throw new InvalidOperationException($"Unexpected binding target '{targetType.Name}'.");
        }

        var targetValue = targetType.GetGenericArguments()[0];
        var classified = SourceClassifier.Classify(source);
        if (classified.Kind is SourceKind.Command or SourceKind.CommandT or SourceKind.List)
        {
            throw new InvalidOperationException(
                $"Cannot bind '{source.GetType().Name}' as a value source.");
        }

        var sourceValue = classified.ValueType;
        var isBindable = classified.Kind == SourceKind.Bindable;

        if (converter is not null)
        {
            InvokeConverterBind(bindings, source, isBindable, sourceValue, target, targetValue, converter);
            return;
        }

        if (sourceValue == targetValue)
        {
            InvokeSameTypeBind(bindings, source, isBindable, sourceValue, target);
            return;
        }

        if (implicitToString && targetValue == typeof(string))
        {
            var convert = typeof(ToStringConverter)
                .GetMethod(nameof(ToStringConverter.Convert))!
                .MakeGenericMethod(sourceValue);
            var funcType = typeof(Func<,>).MakeGenericType(sourceValue, typeof(string));
            var func = Delegate.CreateDelegate(funcType, convert);
            InvokeConvertBind(bindings, source, sourceValue, target, targetValue, func);
            return;
        }

        if (targetValue == typeof(double) && sourceValue == typeof(int))
        {
            InvokeConverterBind(
                bindings,
                source,
                isBindable,
                sourceValue,
                target,
                targetValue,
                IntToDoubleConverter.Instance);
            return;
        }

        if (targetValue == typeof(double) && sourceValue == typeof(float))
        {
            InvokeConverterBind(
                bindings,
                source,
                isBindable,
                sourceValue,
                target,
                targetValue,
                FloatToDoubleConverter.Instance);
            return;
        }

        throw new InvalidOperationException(
            $"Cannot bind '{sourceValue.Name}' to '{targetValue.Name}' without a converter.");
    }

    public static void BindCommand(
        BindingSet bindings,
        object command,
        BaseButton button,
        Node parameterNode,
        Type argumentType)
    {
        var getter = typeof(RuntimeBind)
            .GetMethod(nameof(CreateParameterGetter), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(argumentType)
            .Invoke(null, [parameterNode]);
        typeof(GodotCommands)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method =>
                method.Name == nameof(GodotCommands.BindCommand)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 4)
            .MakeGenericMethod(argumentType)
            .Invoke(null, [bindings, command, button, getter]);
    }

    public static void BindItems(
        BindingSet bindings,
        object source,
        ClassifiedSource classified,
        object target,
        object? converter)
    {
        var targetType = target.GetType();
        if (!targetType.IsGenericType || targetType.GetGenericTypeDefinition() != typeof(CollectionTarget<>))
        {
            throw new InvalidOperationException($"Unexpected collection target '{targetType.Name}'.");
        }

        var targetItem = targetType.GetGenericArguments()[0];
        if (classified.Kind == SourceKind.List)
        {
            if (converter is not null)
            {
                InvokeListItemsConvert(bindings, source, classified.ValueType, target, targetItem, converter);
                return;
            }

            if (classified.ValueType != targetItem)
            {
                throw new InvalidOperationException(
                    $"Cannot bind ObservableList<{classified.ValueType.Name}> to CollectionTarget<{targetItem.Name}> without a converter.");
            }

            typeof(CollectionBindingExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(static method =>
                    method.Name == nameof(CollectionBindingExtensions.BindItems)
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 3
                    && method.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                    == typeof(ObservableList<>))
                .MakeGenericMethod(classified.ValueType)
                .Invoke(null, [bindings, source, target]);
            return;
        }

        if (classified.Kind is SourceKind.Bindable or SourceKind.Observable)
        {
            BindObservableItems(bindings, source, classified.ValueType, target, targetItem, converter);
            return;
        }

        throw new InvalidOperationException(
            $"Items binding requires ObservableList or Observable list, got '{source.GetType().Name}'.");
    }

    public static void BindContainerItems(
        BindingSet bindings,
        object source,
        ClassifiedSource classified,
        Container container,
        PackedScene scene)
    {
        if (classified.Kind != SourceKind.List)
        {
            throw new InvalidOperationException(
                $"Container items require ObservableList, got '{source.GetType().Name}'.");
        }

        if (!typeof(IViewModel).IsAssignableFrom(classified.ValueType))
        {
            throw new InvalidOperationException(
                $"Container item type '{classified.ValueType.Name}' must implement IViewModel.");
        }

        var target = typeof(GodotCollectionTargets)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method =>
                method.Name == nameof(GodotCollectionTargets.Items)
                && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 2
                && method.GetParameters()[1].ParameterType == typeof(PackedScene))
            .MakeGenericMethod(classified.ValueType)
            .Invoke(null, [container, scene]);

        typeof(CollectionBindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method =>
                method.Name == nameof(CollectionBindingExtensions.BindItems)
                && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 3
                && method.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                == typeof(ObservableList<>))
            .MakeGenericMethod(classified.ValueType)
            .Invoke(null, [bindings, source, target]);
    }

    private static Func<T> CreateParameterGetter<T>(Node node)
        => () =>
        {
            var value = ReadParameter(node);
            if (value is T typed)
            {
                return typed;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        };

    private static object ReadParameter(Node node)
        => node switch
        {
            LineEdit edit => edit.Text,
            TextEdit edit => edit.Text,
            OptionButton option => (long)option.Selected,
            global::Godot.Range range => range.Value,
            CheckBox check => check.ButtonPressed,
            CheckButton check => check.ButtonPressed,
            BaseButton button when button.ToggleMode => button.ButtonPressed,
            _ => throw new InvalidOperationException(
                $"Cannot read a command parameter from '{node.GetType().Name}'."),
        };

    private static void InvokeSameTypeBind(
        BindingSet bindings,
        object source,
        bool isBindable,
        Type valueType,
        object target)
    {
        var sourceType = isBindable
            ? typeof(BindableReactiveProperty<>).MakeGenericType(valueType)
            : typeof(Observable<>).MakeGenericType(valueType);
        var targetType = typeof(BindingTarget<>).MakeGenericType(valueType);
        typeof(BindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(BindingExtensions.Bind)
                && method.GetGenericArguments().Length == 1
                && method.GetParameters().Length == 3
                && Matches(method.GetParameters()[1].ParameterType, sourceType)
                && Matches(method.GetParameters()[2].ParameterType, targetType))
            .MakeGenericMethod(valueType)
            .Invoke(null, [bindings, source, target]);
    }

    private static void InvokeConvertBind(
        BindingSet bindings,
        object source,
        Type sourceValue,
        object target,
        Type targetValue,
        Delegate convert)
    {
        var targetType = typeof(BindingTarget<>).MakeGenericType(targetValue);
        var funcType = typeof(Func<,>).MakeGenericType(sourceValue, targetValue);
        typeof(BindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(BindingExtensions.Bind)
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 4
                && Matches(method.GetParameters()[3].ParameterType, funcType))
            .MakeGenericMethod(sourceValue, targetValue)
            .Invoke(null, [bindings, source, target, convert]);
    }

    private static void InvokeConverterBind(
        BindingSet bindings,
        object source,
        bool isBindable,
        Type sourceValue,
        object target,
        Type targetValue,
        object converter)
    {
        var twoWay = typeof(ITwoWayValueConverter<,>).MakeGenericType(sourceValue, targetValue);
        if (isBindable && twoWay.IsInstanceOfType(converter))
        {
            typeof(BindingExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method =>
                    method.Name == nameof(BindingExtensions.Bind)
                    && method.GetGenericArguments().Length == 2
                    && method.GetParameters().Length == 4
                    && method.GetParameters()[3].ParameterType.IsGenericType
                    && method.GetParameters()[3].ParameterType.GetGenericTypeDefinition()
                    == typeof(ITwoWayValueConverter<,>))
                .MakeGenericMethod(sourceValue, targetValue)
                .Invoke(null, [bindings, source, target, converter]);
            return;
        }

        var oneWay = typeof(IValueConverter<,>).MakeGenericType(sourceValue, targetValue);
        if (!oneWay.IsInstanceOfType(converter))
        {
            throw new InvalidOperationException(
                $"Converter '{converter.GetType().Name}' does not convert {sourceValue.Name} to {targetValue.Name}.");
        }

        typeof(BindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(BindingExtensions.Bind)
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 4
                && method.GetParameters()[3].ParameterType.IsGenericType
                && method.GetParameters()[3].ParameterType.GetGenericTypeDefinition()
                == typeof(IValueConverter<,>))
            .MakeGenericMethod(sourceValue, targetValue)
            .Invoke(null, [bindings, source, target, converter]);
    }

    private static void InvokeListItemsConvert(
        BindingSet bindings,
        object source,
        Type sourceItem,
        object target,
        Type targetItem,
        object converter)
    {
        var converterType = typeof(IValueConverter<,>).MakeGenericType(sourceItem, targetItem);
        if (!converterType.IsInstanceOfType(converter))
        {
            throw new InvalidOperationException(
                $"Converter '{converter.GetType().Name}' does not convert list items {sourceItem.Name} to {targetItem.Name}.");
        }

        typeof(CollectionBindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(CollectionBindingExtensions.BindItems)
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 4
                && method.GetParameters()[1].ParameterType.IsGenericType
                && method.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                == typeof(ObservableList<>)
                && method.GetParameters()[3].ParameterType.IsGenericType
                && method.GetParameters()[3].ParameterType.GetGenericTypeDefinition()
                == typeof(IValueConverter<,>))
            .MakeGenericMethod(sourceItem, targetItem)
            .Invoke(null, [bindings, source, target, converter]);
    }

    private static void BindObservableItems(
        BindingSet bindings,
        object source,
        Type listType,
        object target,
        Type targetItem,
        object? converter)
    {
        if (converter is not null)
        {
            throw new InvalidOperationException(
                "Converters on Observable list bindings are not supported for scene bindings.");
        }

        var itemType = GetListItemType(listType);
        if (itemType != targetItem)
        {
            throw new InvalidOperationException(
                $"Cannot bind Observable<{listType.Name}> to CollectionTarget<{targetItem.Name}>.");
        }

        typeof(CollectionBindingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(CollectionBindingExtensions.BindItems)
                && method.GetGenericArguments().Length == 2
                && method.GetParameters().Length == 3)
            .MakeGenericMethod(targetItem, listType)
            .Invoke(null, [bindings, source, target]);
    }

    private static Type GetListItemType(Type listType)
    {
        if (listType.IsGenericType)
        {
            var definition = listType.GetGenericTypeDefinition();
            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IList<>) || definition == typeof(List<>))
            {
                return listType.GetGenericArguments()[0];
            }
        }

        var readOnly = listType.GetInterfaces()
            .FirstOrDefault(static iface =>
                iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
        if (readOnly is not null)
        {
            return readOnly.GetGenericArguments()[0];
        }

        throw new InvalidOperationException($"'{listType.Name}' is not a list type.");
    }

    private static bool Matches(Type parameter, Type constructed)
        => parameter == constructed
            || (parameter.IsGenericType
                && constructed.IsGenericType
                && parameter.GetGenericTypeDefinition() == constructed.GetGenericTypeDefinition());
}
