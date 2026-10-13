using MigrationPlatform.AI.Models;

namespace MigrationPlatform.AI.Interfaces;

public interface IAIProvider
{
    string Name { get; }
    Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken);
}