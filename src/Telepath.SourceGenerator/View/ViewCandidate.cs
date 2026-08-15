using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

internal sealed class ViewCandidate
{
    public ViewCandidate(
        INamedTypeSymbol viewType,
        ITypeSymbol? viewModelType,
        Location location)
    {
        ViewType = viewType;
        ViewModelType = viewModelType;
        Location = location;
    }

    public INamedTypeSymbol ViewType { get; }

    public ITypeSymbol? ViewModelType { get; }

    public Location Location { get; }
}
