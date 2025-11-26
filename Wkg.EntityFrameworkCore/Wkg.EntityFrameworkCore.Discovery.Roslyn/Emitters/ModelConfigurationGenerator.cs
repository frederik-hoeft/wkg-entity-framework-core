using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;
using Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;

internal sealed class ModelConnectionConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public IConfigurationCode GenerateCode(ModelConnection connection) => new ConnectionConfigurationCode(this, connection);

    private sealed class ConnectionConfigurationCode(ModelConnectionConfigurationGenerator generator, ModelConnection connection) : ConfigurationCodeBase
    {
        private string? _leftBuilderInstance;
        private string? _rightBuilderInstance;

        public override INamedTypeSymbol Symbol => connection.Connector;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, IConfigurationCode> allConfigurations)
        {
            if (allConfigurations.TryGetValue(connection.Left, out IConfigurationCode? left))
            {
                _leftBuilderInstance = left.MarkRequired();
            }
            if (allConfigurations.TryGetValue(connection.Right, out IConfigurationCode? right))
            {
                _rightBuilderInstance = right.MarkRequired();
            }
        }

        public override IEnumerable<string> EmitSourceLines()
        {
            string modelFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            StringBuilder builder;
            string concreteConnectionType = $"{generator._types["EntityConnectionLoader"]}<{modelFullName}, {connection.Left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {connection.Right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            if (string.IsNullOrEmpty(InstanceName))
            {
                builder = new StringBuilder();
            }
            else
            {
                builder = new StringBuilder(new string(' ', 4));
                yield return $"{concreteConnectionType} {InstanceName} =";
            }
            if (_leftBuilderInstance is null || _rightBuilderInstance is null)
            {
                throw new InvalidOperationException($"Cannot emit connection configuration for '{modelFullName}' because one or both of its endpoint model configurations are missing.");
            }
            builder.Append(concreteConnectionType).Append(".Configure(builder, ").Append(_leftBuilderInstance).Append(", ").Append(_rightBuilderInstance).Append(").Register(context);");
            yield return builder.ToString();
        }
    }
}

internal abstract class ConfigurationCodeBase : IConfigurationCode
{
    protected string? InstanceName { get; private set; }

    public abstract INamedTypeSymbol Symbol { get; }

    public abstract IEnumerable<string> EmitSourceLines();

    public virtual string MarkRequired() => InstanceName ??= SymbolNameGenerator.MakeUnique(SymbolNameGenerator.MakeCamelCase(Symbol.Name));

    public abstract void ResolveDependencies(FrozenDictionary<ITypeSymbol, IConfigurationCode> allConfigurations);
}

internal sealed class ModelConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public IConfigurationCode GenerateCode(INamedTypeSymbol modelSymbol)
    {
        IEnumerable<INamedTypeSymbol> baseModelSymbols = CompilationExplorer.GetBaseModelConfigurationSymbols(modelSymbol);
        return new ModelConfigurationCode(this, modelSymbol, baseModelSymbols);
    }

    private sealed class ModelConfigurationCode(ModelConfigurationGenerator generator, INamedTypeSymbol modelSymbol, IEnumerable<INamedTypeSymbol> baseModelSymbols) : ConfigurationCodeBase
    {
        public override INamedTypeSymbol Symbol => modelSymbol;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, IConfigurationCode> allConfigurations) { }

        public override IEnumerable<string> EmitSourceLines()
        {
            string modelFullName = Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            StringBuilder builder = string.IsNullOrEmpty(InstanceName)
                ? new()
                : new($"{generator._types["EntityTypeBuilder"]}<{modelFullName}> {InstanceName} = ");
            builder.Append($"{generator._types["EntityLoader"]}<{modelFullName}>.Configure(builder)");
            foreach (INamedTypeSymbol baseModelSymbol in baseModelSymbols)
            {
                builder.Append($".ConfigureBase<{modelFullName}, {baseModelSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()");
            }
            builder.Append(".Register(context);");
            return [builder.ToString()];
        }
    }
}