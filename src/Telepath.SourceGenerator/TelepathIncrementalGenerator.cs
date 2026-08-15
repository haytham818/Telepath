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

        var bindableMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            ViewModelMetadata.BindableAttributeName,
            static (node, _) => node is VariableDeclaratorSyntax or FieldDeclarationSyntax or MethodDeclarationSyntax,
            static (attributeContext, _) => ViewModelGenerator.CreateBindableMember(attributeContext));

        var commandMembers = context.SyntaxProvider.ForAttributeWithMetadataName(
            ViewModelMetadata.CommandAttributeName,
            static (node, _) => node is MethodDeclarationSyntax,
            static (attributeContext, _) => ViewModelGenerator.CreateCommandMember(attributeContext));

        var viewModelMembers = bindableMembers.Collect().Combine(commandMembers.Collect());

        context.RegisterSourceOutput(viewModelMembers, static (productionContext, pair) =>
        {
            ViewModelGenerator.Generate(productionContext, pair.Left, pair.Right);
        });
    }
}
