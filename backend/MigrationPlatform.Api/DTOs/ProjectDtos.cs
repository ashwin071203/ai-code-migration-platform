namespace MigrationPlatform.Api.DTOs;

public sealed record CreateProjectRequest(string Name, string Description);
public sealed record ProjectResponse(Guid Id, string Name, string Description, string SourceLanguage, string Framework, string Status, DateTime CreatedAt);
public sealed record ProjectDetailResponse(Guid Id, string Name, string Description, string SourceLanguage, string Framework, string Status, int FileCount, bool HasAnalysis, DateTime CreatedAt, DateTime UpdatedAt);