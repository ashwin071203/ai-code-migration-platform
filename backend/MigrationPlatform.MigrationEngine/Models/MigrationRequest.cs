namespace MigrationPlatform.MigrationEngine.Models;

public sealed record MigrationRequest(Guid MigrationId, string SourceTechnology, string TargetTechnology, string SourceRoot, string OutputRoot);
public sealed record MigrationResult(string Status, string? OutputRoot, IReadOnlyList<string> Warnings, IReadOnlyList<string> FilesProcessed, string? ErrorMessage);