using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal enum ViewModelMemberKind
{
    BindableField,
    BindableListField,
    BindableMethod,
    Command,
}

internal sealed class ViewModelMemberCandidate
{
    public ViewModelMemberCandidate(
        INamedTypeSymbol? containingType,
        ISymbol? member,
        ViewModelMemberKind kind,
        ImmutableArray<string> from,
        string? canExecute,
        string? explicitName,
        Location location)
    {
        ContainingType = containingType;
        Member = member;
        Kind = kind;
        From = from;
        CanExecute = canExecute;
        ExplicitName = explicitName;
        Location = location;
    }

    public INamedTypeSymbol? ContainingType { get; }

    public ISymbol? Member { get; }

    public ViewModelMemberKind Kind { get; }

    public ImmutableArray<string> From { get; }

    public string? CanExecute { get; }

    public string? ExplicitName { get; }

    public Location Location { get; }
}
