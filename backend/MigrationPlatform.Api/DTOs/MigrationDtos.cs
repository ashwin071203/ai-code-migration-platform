namespace MigrationPlatform.Api.DTOs;

public sealed record CreateMigrationRequest(Guid ProjectId, string SourceTechnology, string TargetTechnology);
public sealed record MigrationResponse(Guid Id, Guid ProjectId, string SourceTechnology, string TargetTechnology, string Status, int Progress, string? ErrorMessage, bool HasOutput, DateTime CreatedAt);