namespace MigrationPlatform.Api.DTOs;

public sealed record ValidationResponse(Guid MigrationId, string Status, int FilesChecked, int Errors, int Warnings, string? Summary, DateTime CreatedAt);