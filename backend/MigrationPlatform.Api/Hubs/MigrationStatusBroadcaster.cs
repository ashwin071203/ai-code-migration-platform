using MigrationPlatform.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Api.Hubs;

public sealed class MigrationStatusBroadcaster(IServiceScopeFactory scopeFactory, IHubContext<MigrationHub> hub, ILogger<MigrationStatusBroadcaster> logger) : BackgroundService
{
    private readonly Dictionary<Guid, (string Status, int Progress, string? Error)> lastSeen = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MigrationDbContext>();
                var jobs = await db.MigrationJobs.AsNoTracking().Select(job => new { job.Id, job.ProjectId, job.SourceTechnology, job.TargetTechnology, job.Status, job.Progress, job.ErrorMessage, job.OutputStorageKey, job.CreatedAt }).ToListAsync(stoppingToken);
                foreach (var job in jobs)
                {
                    var current = (job.Status, job.Progress, job.ErrorMessage);
                    if (lastSeen.TryGetValue(job.Id, out var previous) && previous == current) continue;
                    lastSeen[job.Id] = current;
                    await hub.Clients.Group(job.Id.ToString()).SendAsync("MigrationUpdated", new { id = job.Id, projectId = job.ProjectId, sourceTechnology = job.SourceTechnology, targetTechnology = job.TargetTechnology, status = job.Status, progress = job.Progress, errorMessage = job.ErrorMessage, hasOutput = job.OutputStorageKey is not null, createdAt = job.CreatedAt }, stoppingToken);
                }
                var activeIds = jobs.Select(job => job.Id).ToHashSet();
                foreach (var staleId in lastSeen.Keys.Where(id => !activeIds.Contains(id)).ToArray()) lastSeen.Remove(staleId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogWarning(exception, "Migration status broadcast cycle failed."); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}