using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

internal static class IncrementalGeneratorPostInitializationContextExtensions
{
    extension(IncrementalGeneratorPostInitializationContext self)
    {
        public void AddEmbeddedSource<T>()
        {
            SourceText sourceText = SourceTextExtensions.FromEmbedded<T>();
            self.AddSource($"{typeof(T).FullName}.cs", sourceText);
        }

        public void AddEmbeddedSource<T>(string typeName)
        {
            SourceText sourceText = SourceTextExtensions.FromEmbedded<T>(typeName);
            self.AddSource($"{typeof(T).Namespace}.{typeName}.cs", sourceText);
        }
    }
}