namespace MigrationPlatform.AI.Models;

public sealed record AIRequest(string SystemPrompt, string UserPrompt, string? Model = null);