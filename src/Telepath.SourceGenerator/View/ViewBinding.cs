namespace Telepath.SourceGenerator;

internal enum BindToKind
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

internal sealed class NodeInjection
{
    public NodeInjection(string targetMemberName, string nodePath, string nodeTypeDisplay)
    {
        TargetMemberName = targetMemberName;
        NodePath = nodePath;
        NodeTypeDisplay = nodeTypeDisplay;
    }

    public string TargetMemberName { get; }

    public string NodePath { get; }

    public string NodeTypeDisplay { get; }
}

internal sealed class ViewBinding
{
    public ViewBinding(
        string targetMemberName,
        string viewModelMember,
        BindToKind kind,
        string? parameterMemberName = null,
        string? parameterAccess = null,
        string? converterTypeDisplay = null,
        bool implicitToString = false)
    {
        TargetMemberName = targetMemberName;
        ViewModelMember = viewModelMember;
        Kind = kind;
        ParameterMemberName = parameterMemberName;
        ParameterAccess = parameterAccess;
        ConverterTypeDisplay = converterTypeDisplay;
        ImplicitToString = implicitToString;
    }

    public string TargetMemberName { get; }

    public string ViewModelMember { get; }

    public BindToKind Kind { get; }

    public string? ParameterMemberName { get; }

    public string? ParameterAccess { get; }

    public string? ConverterTypeDisplay { get; }

    public bool ImplicitToString { get; }

    public string TargetAccessor => Kind switch
    {
        BindToKind.Text => ".Text()",
        BindToKind.Toggle => ".Toggle()",
        BindToKind.Value => ".Value()",
        BindToKind.Selected => ".Selected()",
        BindToKind.Visible => ".Visible()",
        BindToKind.Disabled => ".Disabled()",
        _ => throw new InvalidOperationException($"Unsupported BindTo kind '{Kind}'."),
    };
}
