using MigrationPlatform.MigrationEngine.Interfaces;
using MigrationPlatform.MigrationEngine.Providers;

namespace MigrationPlatform.MigrationEngine.Implementations;

public sealed class MigrationProviderFactory : IMigrationProviderFactory
{
    private readonly IReadOnlyList<IMigrationProvider> providers = [new DotNetModernizationProvider(), new JavaToCSharpProvider(), new SpringToAspNetProvider(), new ReactToReactNativeProvider()];
    public IMigrationProvider? Resolve(string sourceTechnology, string targetTechnology) => providers.FirstOrDefault(provider => provider.CanHandle(sourceTechnology, targetTechnology));
}