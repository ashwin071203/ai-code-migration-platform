namespace MigrationPlatform.AI.Interfaces;

public interface IAIProviderFactory
{
    IAIProvider Resolve();
}