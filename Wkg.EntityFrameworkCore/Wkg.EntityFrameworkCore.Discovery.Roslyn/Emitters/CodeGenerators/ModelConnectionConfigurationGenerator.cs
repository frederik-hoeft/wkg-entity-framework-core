using Microsoft.CodeAnalysis;
using System.Collections.Frozen;
using System.Text;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters.CodeGenerators;

internal sealed class ModelConnectionConfigurationGenerator(FrozenDictionary<string, string> types)
{
    private readonly FrozenDictionary<string, string> _types = types;

    public INamedConfigurationCode GenerateCode(ModelConnection connection) => new ConnectionConfigurationCode(this, connection);

    private sealed class ConnectionConfigurationCode(ModelConnectionConfigurationGenerator generator, ModelConnection connection) : NamedConfigurationCodeBase
    {
        private string? _leftBuilderInstance;
        private string? _rightBuilderInstance;

        public override INamedTypeSymbol Symbol => connection.Connector;

        public override void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations)
        {
            if (allConfigurations.TryGetValue(connection.Left, out INamedConfigurationCode? left))
            {
                _leftBuilderInstance = left.MarkRequired();
            }
            if (allConfigurations.TryGetValue(connection.Right, out INamedConfigurationCode? right))
            {
                _rightBuilderInstance = right.MarkRequired();
            }
        }

        public override IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context)
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
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "WKGDF0010",
                        title: "Missing Model Configuration",
                        messageFormat: "Cannot emit connection configuration for '{0}' because one or both of the entities it connects are missing: ensure that '{1}' and '{2}' are also configured within the same discovery context.",
                        category: "Wkg.EntityFrameworkCore.Discovery",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true),
                    location: source.Locations.FirstOrDefault(),
                    Symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    connection.Left.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    connection.Right.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
            builder.Append(concreteConnectionType).Append(".Configure(builder, ").Append(_leftBuilderInstance).Append(", ").Append(_rightBuilderInstance).Append(").Register(context);");
            yield return builder.ToString();
        }
    }
}
