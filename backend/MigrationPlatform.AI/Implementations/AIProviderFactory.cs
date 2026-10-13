using MigrationPlatform.AI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MigrationPlatform.AI.Implementations;

public sealed class AIProviderFactory(IServiceProvider services, IConfiguration configuration) : IAIProviderFactory
{
    public IAIProvider Resolve()
    {
        var provider = configuration["AI:Provider"] ?? "Ollama";
        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)) return services.GetRequiredService<OpenAICompatibleProvider>();
        if (provider.Equals("DevelopmentFallback", StringComparison.OrdinalIgnoreCase)) return services.GetRequiredService<DevelopmentFallbackProvider>();
        return services.GetRequiredService<OllamaAIProvider>();
    }
}