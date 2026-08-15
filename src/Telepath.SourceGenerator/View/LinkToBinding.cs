namespace Telepath.SourceGenerator;

internal enum LinkToKind
{
    Auto = 0,
    Text,
    Command,
    Toggle,
    Value,
    Selected,
    Visible,
    Disabled,
}

internal sealed class LinkToBinding
{
    public LinkToBinding(
        string targetMemberName,
        string nodePath,
        string viewModelMember,
        string nodeTypeDisplay,
        LinkToKind kind)
    {
        TargetMemberName = targetMemberName;
        NodePath = nodePath;
        ViewModelMember = viewModelMember;
        NodeTypeDisplay = nodeTypeDisplay;
        Kind = kind;
    }

    public string TargetMemberName { get; }

    public string NodePath { get; }

    public string ViewModelMember { get; }

    public string NodeTypeDisplay { get; }

    public LinkToKind Kind { get; }

    public string BindMethodName => Kind switch
    {
        LinkToKind.Text => "BindText",
        LinkToKind.Command => "BindCommand",
        LinkToKind.Toggle => "BindToggle",
        LinkToKind.Value => "BindValue",
        LinkToKind.Selected => "BindSelected",
        LinkToKind.Visible => "BindVisible",
        LinkToKind.Disabled => "BindDisabled",
        _ => throw new InvalidOperationException($"Unsupported LinkTo kind '{Kind}'."),
    };
}
