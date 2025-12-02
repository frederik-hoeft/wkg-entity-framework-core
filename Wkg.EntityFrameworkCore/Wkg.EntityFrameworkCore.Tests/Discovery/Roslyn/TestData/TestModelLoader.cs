using Wkg.EntityFrameworkCore.Discovery.Roslyn;

namespace Wkg.EntityFrameworkCore.Tests.Discovery.Roslyn.TestData;

[ModelLoader(AssemblyDiscoveryFailureBehavior = AssemblyDiscoveryFailureBehavior.Error)]
internal sealed partial class TestModelLoader;