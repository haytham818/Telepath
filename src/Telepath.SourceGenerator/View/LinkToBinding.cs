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
        LinkToKind kind,
        string? parameterMemberName = null,
        string? parameterAccess = null,
        string? converterTypeDisplay = null,
        bool implicitToString = false)
    {
        TargetMemberName = targetMemberName;
        NodePath = nodePath;
        ViewModelMember = viewModelMember;
        NodeTypeDisplay = nodeTypeDisplay;
        Kind = kind;
        ParameterMemberName = parameterMemberName;
        ParameterAccess = parameterAccess;
        ConverterTypeDisplay = converterTypeDisplay;
        ImplicitToString = implicitToString;
    }

    public string TargetMemberName { get; }

    public string NodePath { get; }

    public string ViewModelMember { get; }

    public string NodeTypeDisplay { get; }

    public LinkToKind Kind { get; }

    public string? ParameterMemberName { get; }

    public string? ParameterAccess { get; }

    public string? ConverterTypeDisplay { get; }

    public bool ImplicitToString { get; }

    public string TargetAccessor => Kind switch
    {
        LinkToKind.Text => ".Text()",
        LinkToKind.Toggle => ".Toggle()",
        LinkToKind.Value => ".Value()",
        LinkToKind.Selected => ".Selected()",
        LinkToKind.Visible => ".Visible()",
        LinkToKind.Disabled => ".Disabled()",
        _ => throw new InvalidOperationException($"Unsupported LinkTo kind '{Kind}'."),
    };
}
