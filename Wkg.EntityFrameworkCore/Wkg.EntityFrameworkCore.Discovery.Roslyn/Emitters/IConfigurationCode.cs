using Microsoft.CodeAnalysis;
using System.Collections.Frozen;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;

internal interface IConfigurationCode
{
    INamedTypeSymbol Symbol { get; }

    string MarkRequired();

    void ResolveDependencies(FrozenDictionary<ITypeSymbol, IConfigurationCode> allConfigurations);

    IEnumerable<string> EmitSourceLines();
}
