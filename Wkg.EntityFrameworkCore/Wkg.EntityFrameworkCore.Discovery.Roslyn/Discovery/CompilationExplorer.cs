using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Discovery;

internal sealed class CompilationExplorer(Compilation compilation, ModelDiscoveryContext discoveryContext)
{
    private const string MODEL_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConfiguration<>";
    private const string MODEL_CONNECTION_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelConnection<,,>";
    private const string BASE_MODEL_CONFIGURATION_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableBaseModelConfiguration<>";
    private const string DATA_SEED_MARKER_INTERFACE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.IDiscoverableModelDataSeed<>";

    public ModelDiscoveryContext DiscoveryContext => discoveryContext;

    private IEnumerable<INamedTypeSymbol> GetCandidateTypes(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> allTypes;
        if (discoveryContext.Attribute.TargetAssemblies is { Length: > 0 } targetAssemblies)
        {
            // ensure that the target assemblies even exist in the compilation
            Dictionary<string, List<IAssemblySymbol>> assemblies = compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .Union([source.ContainingAssembly], SymbolEqualityComparer.Default)
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<IAssemblySymbol>()
                .GroupBy(asm => asm.Name)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            foreach (string assemblyName in targetAssemblies)
            {
                if (!assemblies.ContainsKey(assemblyName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "EFDR001",
                            title: "Target Assembly Not Found",
                            messageFormat: "The target assembly '{0}' specified in the ModelLoaderAttribute could not be found in the compilation.",
                            category: "ModelDiscovery",
                            discoveryContext.GetDiagnosticSeverity(),
                            isEnabledByDefault: true),
                        source.Locations.FirstOrDefault(),
                        assemblyName));
                }
            }
            allTypes = targetAssemblies.SelectMany(target =>
            {
                if (assemblies.TryGetValue(target, out List<IAssemblySymbol>? matches))
                {
                    return matches;
                }
                return [];
            }).SelectMany(asm => asm.GlobalNamespace.GetAllTypes());
        }
        else
        {
            allTypes = compilation.Assembly.GlobalNamespace.GetAllTypes();
        }
        if (discoveryContext.FilterAttributeData.Length > 0)
        {
            HashSet<ITypeSymbol> filterAttributeTypes = new(discoveryContext.GetFilterAttributeTypes(source, context), SymbolEqualityComparer.Default);
            allTypes = allTypes.Where(type => type.GetAttributes().Any(attr => attr.AttributeClass is not null && filterAttributeTypes.Contains(attr.AttributeClass)));
        }
        return allTypes;
    }

    public IEnumerable<INamedTypeSymbol> DiscoverModels(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<INamedTypeSymbol> models = [];
        foreach (INamedTypeSymbol type in candidateTypes)
        {
            if (!type.IsReferenceType)
            {
                continue;
            }
            INamedTypeSymbol? modelInterface = type.Interfaces.FirstOrDefault(iface =>
                iface.IsGenericType
                && iface.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == MODEL_MARKER_INTERFACE_FULL_NAME
                && iface.TypeArguments is [ITypeSymbol self]
                && SymbolEqualityComparer.Default.Equals(self, type));
            if (modelInterface is null)
            {
                continue;
            }
            models.Add(type);
            discoveryContext.OnTypeDiscovered(type);
        }
        return models;
    }

    public IEnumerable<ModelConnection> DiscoverModelConnections(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<ModelConnection> connections = [];
        foreach (INamedTypeSymbol type in candidateTypes)
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
                discoveryContext.OnTypeDiscovered(type);
            }
        }
        return connections;
    }

    public IEnumerable<ModelDataSeed> DiscoverDataSeeds(ISymbol source, SourceProductionContext context)
    {
        IEnumerable<INamedTypeSymbol> candidateTypes = GetCandidateTypes(source, context);
        List<ModelDataSeed> dataSeeds = [];
        foreach (INamedTypeSymbol type in candidateTypes)
        {
            if (type.Interfaces.FirstOrDefault(i => i.IsGenericType
                && i.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == DATA_SEED_MARKER_INTERFACE_FULL_NAME) is
                {
                    TypeArguments: [ITypeSymbol model]
                })
            {
                dataSeeds.Add(new ModelDataSeed
                (
                    Seeder: type,
                    Model: model
                ));
                discoveryContext.OnTypeDiscovered(type);
            }
        }
        return dataSeeds;
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
