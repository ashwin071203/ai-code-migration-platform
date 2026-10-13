namespace MigrationPlatform.Api.DTOs;

public sealed record AICompletionRequest(string Prompt, string? SystemPrompt = null, string? Model = null);