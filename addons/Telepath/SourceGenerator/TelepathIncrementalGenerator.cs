using Microsoft.CodeAnalysis;

namespace Telepath.SourceGenerator;

/// <summary>
/// Placeholder incremental generator. Concrete generators will be added later.
/// </summary>
[Generator]
public sealed class TelepathIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Intentionally empty: scaffold only.
    }
}
