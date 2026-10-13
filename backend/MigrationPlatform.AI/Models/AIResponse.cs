namespace MigrationPlatform.AI.Models;

public sealed record AIResponse(string Content, string Provider, string Model, int? PromptTokens, int? CompletionTokens);