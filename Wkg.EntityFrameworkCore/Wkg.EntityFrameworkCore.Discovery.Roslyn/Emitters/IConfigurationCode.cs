using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;

internal interface IConfigurationCode
{
    IEnumerable<string> EmitSourceLines(ISymbol source, SourceProductionContext context);

    void ResolveDependencies(FrozenDictionary<ITypeSymbol, INamedConfigurationCode> allConfigurations);
}
