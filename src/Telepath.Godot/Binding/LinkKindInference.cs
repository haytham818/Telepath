using Godot;

namespace Telepath.Godot;

/// <summary>
/// Runtime <see cref="LinkKind"/> inference and compatibility, matching
/// the source generator's control-type rules.
/// </summary>
public static class LinkKindInference
{
    public static bool TryInfer(Node node, out LinkKind kind)
    {
        ArgumentNullException.ThrowIfNull(node);
        kind = node switch
        {
            CheckBox or CheckButton => LinkKind.Toggle,
            OptionButton => LinkKind.Selected,
            ItemList => LinkKind.Items,
            BaseButton => LinkKind.Command,
            Label or RichTextLabel or LineEdit or TextEdit => LinkKind.Text,
            global::Godot.Range => LinkKind.Value,
            _ => LinkKind.Auto,
        };

        return kind != LinkKind.Auto;
    }

    public static LinkKind Resolve(LinkKind kind, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (kind == LinkKind.Auto)
        {
            if (TryInfer(node, out var inferred))
            {
                return inferred;
            }

            throw new InvalidOperationException(
                $"Cannot infer a binding kind for '{node.GetType().Name}'; set Kind explicitly.");
        }

        if (!IsCompatible(kind, node))
        {
            throw new InvalidOperationException(
                $"Kind.{kind} is not valid for '{node.GetType().Name}'.");
        }

        return kind;
    }

    public static bool IsCompatible(LinkKind kind, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return kind switch
        {
            LinkKind.Auto => TryInfer(node, out _),
            LinkKind.Text => node is Label or RichTextLabel or LineEdit or TextEdit,
            LinkKind.Command => node is BaseButton or LineEdit,
            LinkKind.Toggle or LinkKind.Disabled => node is BaseButton,
            LinkKind.Value => node is global::Godot.Range,
            LinkKind.Selected => node is OptionButton or ItemList,
            LinkKind.Items => node is ItemList or OptionButton or Container,
            LinkKind.Visible => node is CanvasItem,
            _ => false,
        };
    }

    public static bool IsLabelText(Node node)
        => node is Label or RichTextLabel;
}
