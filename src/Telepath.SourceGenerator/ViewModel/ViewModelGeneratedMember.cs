using System.Collections.Immutable;

namespace Telepath.SourceGenerator;

internal sealed class ViewModelGeneratedMember
{
    public ViewModelGeneratedMember(
        ViewModelMemberKind kind,
        string propertyName,
        string sourceMemberName,
        string valueTypeDisplay,
        ImmutableArray<string> from,
        string? canExecute,
        bool canExecuteIsMethod,
        string? commandParameterTypeDisplay = null)
    {
        Kind = kind;
        PropertyName = propertyName;
        SourceMemberName = sourceMemberName;
        ValueTypeDisplay = valueTypeDisplay;
        From = from;
        CanExecute = canExecute;
        CanExecuteIsMethod = canExecuteIsMethod;
        CommandParameterTypeDisplay = commandParameterTypeDisplay;
    }

    public ViewModelMemberKind Kind { get; }

    public string PropertyName { get; }

    public string SourceMemberName { get; }

    public string ValueTypeDisplay { get; }

    public ImmutableArray<string> From { get; }

    public string? CanExecute { get; }

    public bool CanExecuteIsMethod { get; }

    public string? CommandParameterTypeDisplay { get; }
}
