using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Wkg.EntityFrameworkCore.Discovery.Roslyn.Helpers;

internal static class SourceTextExtensions
{
    extension(SourceText)
    {
        public static SourceText FromEmbedded<T>()
        {
            if (typeof(T).Assembly != typeof(SourceTextExtensions).Assembly)
            {
                throw new InvalidOperationException("The type T must be defined in the same assembly as SourceTextExtensions.");
            }
            using Stream stream = typeof(T).Assembly.GetManifestResourceStream($"{typeof(T).FullName}.cs")
                ?? throw new InvalidOperationException($"The embedded resource '{typeof(T).FullName}.cs' was not found.");
            return SourceText.From(stream, Encoding.UTF8, canBeEmbedded: true);
        }
    }
}
