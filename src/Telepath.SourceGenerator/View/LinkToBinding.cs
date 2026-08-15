namespace Telepath.SourceGenerator;

internal enum LinkToKind
{
    Label,
    Command,
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
}
