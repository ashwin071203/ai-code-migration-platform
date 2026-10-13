namespace MigrationPlatform.MigrationEngine.Interfaces;

public interface IMigrationProviderFactory
{
    IMigrationProvider? Resolve(string sourceTechnology, string targetTechnology);
}