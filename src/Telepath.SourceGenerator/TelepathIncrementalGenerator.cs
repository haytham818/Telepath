using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Telepath.SourceGenerator;

[Generator]
public sealed class TelepathIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var views = context.SyntaxProvider.ForAttributeWithMetadataName(
            ViewMetadata.ViewAttributeName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (attributeContext, _) => ViewGenerator.CreateCandidate(attributeContext));

        context.RegisterSourceOutput(views, static (productionContext, candidate) =>
        {
            ViewGenerator.Generate(productionContext, candidate);
        });
    }
}
