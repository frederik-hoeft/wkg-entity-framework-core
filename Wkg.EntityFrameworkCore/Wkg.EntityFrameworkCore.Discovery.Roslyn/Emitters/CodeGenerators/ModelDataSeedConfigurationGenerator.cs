using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters.CodeGenerators;

internal sealed class ModelDataSeedConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public INamedConfigurationCode GenerateCode(ModelDataSeed dataSeed) => new DataSeedConfigurationCode(this, dataSeed);

    private sealed class DataSeedConfigurationCode(ModelDataSeedConfigurationGenerator generator, ModelDataSeed dataSeed) : NamedConfigurationCodeBase
    {
        private string? _modelBuilderInstance;

        public override INamedTypeSymbol Symbol => dataSeed.Seeder;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations)
        {
            if (allConfigurations.TryGetValue(dataSeed.Model, out INamedConfigurationCode? model))
            {
                _modelBuilderInstance = model.MarkRequired();
            }
        }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
        {
            string seederFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string modelFullName = dataSeed.Model.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (_modelBuilderInstance is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "WKGDF001",
                        title: "Missing Model Configuration",
                        messageFormat: "Cannot emit data seed configuration for '{0}' because the entity it seeds was never configured. Ensure that entity '{1}' is loaded within the same discovery context.",
                        category: "ModelDiscovery",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location: source.Locations.FirstOrDefault(),
                    Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    dataSeed.Model.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            yield return $"{generator._types["EntityDataSeedLoader"]}<{modelFullName}, {seederFullName}>.Configure({_modelBuilderInstance});";
        }
    }
}
