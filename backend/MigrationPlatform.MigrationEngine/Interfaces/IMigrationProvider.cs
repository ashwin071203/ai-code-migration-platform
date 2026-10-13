using MigrationPlatform.MigrationEngine.Models;

namespace MigrationPlatform.MigrationEngine.Interfaces;

public interface IMigrationProvider
{
    bool CanHandle(string sourceTechnology, string targetTechnology);
    Task<MigrationResult> ExecuteAsync(MigrationRequest request, CancellationToken cancellationToken);
}