namespace MigrationPlatform.Api.DTOs;

public sealed record MigrationRuleRequest(string Name, string SourceTechnology, string TargetTechnology, string Pattern, string Replacement, string Description, int Priority, bool Enabled);
public sealed record MigrationRuleResponse(Guid Id, string Name, string SourceTechnology, string TargetTechnology, string Pattern, string Replacement, string Description, int Priority, bool Enabled, DateTime UpdatedAt);