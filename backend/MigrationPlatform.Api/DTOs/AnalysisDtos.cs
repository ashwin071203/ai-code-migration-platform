namespace MigrationPlatform.Api.DTOs;

public sealed record SourceProfileResponse(string Language, string LanguageVersion, string Framework, string FrameworkVersion, string ProjectType, string BuildTool, int Files, int Dependencies, IReadOnlyList<string> Evidence);