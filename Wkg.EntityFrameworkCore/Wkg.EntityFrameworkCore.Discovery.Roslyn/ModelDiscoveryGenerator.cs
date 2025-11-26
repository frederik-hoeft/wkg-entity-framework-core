using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn;

[Generator(LanguageNames.CSharp)]
public sealed class ModelDiscoveryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitModelDiscoverySource);

        IncrementalValuesProvider<ModelDiscoveryGeneratorModel> pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            typeof(ModelDiscoveryAttribute).FullName,
            predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
            transform: static (context, _) =>
            {
                ISymbol targetClass = context.TargetSymbol;
                CompilationExplorer explorer = new(context.SemanticModel.Compilation);

                return new ModelDiscoveryGeneratorModel
                (
                    Namespace: targetClass.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
                    Class: targetClass, 
                    Models: explorer.DiscoverModels(),
                    ModelConnections: explorer.DiscoverModelConnections()
                );
            }
        );
        context.RegisterSourceOutput(pipeline, ModelDiscoveryEmitter.EmitSource);
    }

    private static void EmitModelDiscoverySource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddEmbeddedSource<ModelDiscoveryAttribute>();
    }
}

internal sealed record ModelDiscoveryGeneratorModel(string Namespace, ISymbol Class, ImmutableArray<INamedTypeSymbol> Models, ImmutableArray<ModelConnection> ModelConnections);

internal sealed record ModelConnection(INamedTypeSymbol Connector, ITypeSymbol Left, ITypeSymbol Right);