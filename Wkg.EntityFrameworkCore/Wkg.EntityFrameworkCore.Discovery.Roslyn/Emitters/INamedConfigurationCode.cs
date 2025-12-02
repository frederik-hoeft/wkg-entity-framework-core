using Microsoft.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Emitters;

internal interface INamedConfigurationCode : IConfigurationCode
{
    INamedTypeSymbol Symbol { get; }

    string MarkRequired();
}
