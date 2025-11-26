using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn;

internal sealed class CompilationExplorer(Compilation compilation)
{
    private const string MODEL_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<>";
    private const string MODEL_CONNECTION_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConnection<,,>";
    private const string BASE_MODEL_CONFIGURATION_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableBaseModelConfiguration<>";

    public ImmutableArray<INamedTypeSymbol> DiscoverModels()
    {
        return
        [
            .. compilation.Assembly.GlobalNamespace.GetAllTypes()
                // TODO: check for DbEngineModelAttributes if specified 
                .Where(type => type.IsReferenceType && type.Interfaces.FirstOrDefault(i => i.IsGenericType 
                    && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == MODEL_MARKER_INTERFACE_FULL_NAME) is
                    {
                        TypeArguments: [ITypeSymbol self]
                    } && SymbolEqualityComparer.Default.Equals(self, type))
        ];
    }

    public ImmutableArray<ModelConnection> DiscoverModelConnections()
    {
        List<ModelConnection> connections = [];

        foreach (INamedTypeSymbol type in compilation.Assembly.GlobalNamespace.GetAllTypes())
        {
            if (type.IsReferenceType && type.Interfaces.FirstOrDefault(i => i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == MODEL_CONNECTION_MARKER_INTERFACE_FULL_NAME) is
                {
                    TypeArguments: [ITypeSymbol self, ITypeSymbol left, ITypeSymbol right]
                } && SymbolEqualityComparer.Default.Equals(self, type))
            {
                connections.Add(new ModelConnection
                (
                    Connector: type,
                    Left: left,
                    Right: right
                ));
            }
        }
        return [.. connections];
    }

    public static IEnumerable<INamedTypeSymbol> GetBaseModelConfigurationSymbols(INamedTypeSymbol modelSymbol)
    {
        List<INamedTypeSymbol> baseConfigurations = [];
        
        // Traverse up the inheritance hierarchy
        for (INamedTypeSymbol? type = modelSymbol.BaseType; type is { SpecialType: not SpecialType.System_Object }; type = type.BaseType)
        {
            // Check if this base type implements IDiscoverableBaseModelConfiguration<T> where T is the base type itself
            INamedTypeSymbol? baseConfigurationInterface = type.Interfaces.FirstOrDefault(i => 
                i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == BASE_MODEL_CONFIGURATION_INTERFACE_FULL_NAME
                && i.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            
            if (baseConfigurationInterface is not null)
            {
                baseConfigurations.Add(type);
            }
        }
        
        return baseConfigurations;
    }
}
