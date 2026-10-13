namespace MigrationPlatform.Api.DTOs;

public sealed record MigrationOption(string Type, string Source, string Target, string Description);
public sealed record MigrationPlanResponse(Guid MigrationId, string Source, string Target, string Status, IReadOnlyList<string> Steps, IReadOnlyList<string> Risks, DateTime CreatedAt);