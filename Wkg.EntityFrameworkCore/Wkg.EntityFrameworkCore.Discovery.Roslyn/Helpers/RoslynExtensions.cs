using Microsoft.CodeAnalysis;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

internal static class RoslynExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetAllTypes(this INamespaceSymbol namespaceSymbol)
    {
        Stack<INamespaceSymbol> remaining = [];
        remaining.Push(namespaceSymbol);
        while (remaining.Count > 0)
        {
            namespaceSymbol = remaining.Pop();
            foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
            {
                yield return type;
            }
            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                remaining.Push(nestedNamespace);
            }
        }
    }
}