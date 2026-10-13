using MigrationPlatform.AI.Interfaces;
using MigrationPlatform.AI.Models;

namespace MigrationPlatform.AI.Implementations;

public sealed class DevelopmentFallbackProvider : IAIProvider
{
    public string Name => "Development fallback";

    public Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = "The local AI provider is not connected, so no model response was generated. " +
                      "Start Ollama with `ollama serve`, pull the configured model with `ollama pull llama3.2`, " +
                      "then set AI:Provider to Ollama. Your prompt was received and is ready to retry.";
        return Task.FromResult(new AIResponse(content, Name, "none", null, null));
    }
}