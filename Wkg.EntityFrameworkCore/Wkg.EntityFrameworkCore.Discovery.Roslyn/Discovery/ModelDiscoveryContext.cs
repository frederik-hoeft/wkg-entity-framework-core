using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Reflection;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Discovery;

internal sealed class ModelDiscoveryContext(ModelLoaderAttribute attribute, ImmutableArray<AttributeData> filterAttributeData)
{
    private const string DATABASE_ENGINE_MODEL_ATTRIBUTE_FULL_NAME = "global::Wkg.EntityFrameworkCore.Configuration.Reflection.Attributes.DatabaseEngineModelAttribute";

    private readonly Dictionary<string, int>? _assemblyTypeCounts = attribute.TargetAssemblies is { Length: > 0 } targetAssemblies
        ? targetAssemblies.ToDictionary(name => name, _ => 0)
        : null;
    private int _loadedTypeCount = 0;

    public ModelLoaderAttribute Attribute => attribute;

    public ImmutableArray<AttributeData> FilterAttributeData => filterAttributeData;

    public IEnumerable<ITypeSymbol> GetFilterAttributeTypes(ISymbol source, SourceProductionContext context)
    {
        foreach (AttributeData filterAttributeData in filterAttributeData)
        {
            if (filterAttributeData.AttributeClass is { TypeArguments: [ITypeSymbol filterTypeSymbol] })
            {
                // ensure the attribute type derives from DatabaseEngineModelAttribute
                bool isValid = false;
                for (INamedTypeSymbol? baseType = filterTypeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
                {
                    if (baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == DATABASE_ENGINE_MODEL_ATTRIBUTE_FULL_NAME)
                    {
                        isValid = true;
                        break;
                    }
                }
                if (!isValid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "WKGEFCDR003",
                            title: "Invalid model discovery filter attribute",
                            messageFormat: "The type argument '{0}' of the model discovery filter attribute must derive from DatabaseEngineModelAttribute.",
                            category: "Design",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        source.Locations[0],
                        filterTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    continue;
                }
                yield return filterTypeSymbol;
            }
        }
    }

    public void ReportDiscoveryResults(ISymbol source, SourceProductionContext context)
    {
        if (_loadedTypeCount is 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "WKGEFCDR001",
                    title: "No discoverable models found",
                    messageFormat: "No discoverable models implementing IDiscoverableModelConfiguration<T> were found in the specified assemblies.",
                    category: "Design",
                    defaultSeverity: attribute.GetDiagnosticSeverity(),
                    isEnabledByDefault: true),
                source.Locations[0]));
        }
        if (_assemblyTypeCounts is not null)
        {
            foreach (KeyValuePair<string, int> kvp in _assemblyTypeCounts)
            {
                string assemblyName = kvp.Key;
                int typeCount = kvp.Value;
                if (typeCount is 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "WKGEFCDR002",
                            title: "No discoverable models found in assembly",
                            messageFormat: "No discoverable models implementing IDiscoverableModelConfiguration<T> were found in assembly '{0}'.",
                            category: "Design",
                            defaultSeverity: attribute.GetDiagnosticSeverity(),
                            isEnabledByDefault: true),
                        source.Locations[0],
                        assemblyName));
                }
            }
        }
    }

    public DiagnosticSeverity GetDiagnosticSeverity() => attribute.GetDiagnosticSeverity();

    public void OnTypeDiscovered(INamedTypeSymbol type)
    {
        ++_loadedTypeCount;
        if (_assemblyTypeCounts is not null)
        {
            ++_assemblyTypeCounts[type.ContainingAssembly.Name];
        }
    }
}