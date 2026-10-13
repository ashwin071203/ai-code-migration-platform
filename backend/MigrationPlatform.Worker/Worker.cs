using MigrationPlatform.Data;
using MigrationPlatform.MigrationEngine.Interfaces;
using MigrationPlatform.MigrationEngine.Models;
using MigrationPlatform.Services;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Worker;

public class Worker(IServiceScopeFactory scopeFactory, IMigrationProviderFactory providers, IConfiguration configuration, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MigrationDbContext>();
                var candidateId = await db.MigrationJobs.Where(candidate => candidate.Status == "Queued").OrderBy(candidate => candidate.CreatedAt).Select(candidate => candidate.Id).FirstOrDefaultAsync(stoppingToken);
                if (candidateId != Guid.Empty)
                {
                    var claimed = await db.MigrationJobs
                        .Where(candidate => candidate.Id == candidateId && candidate.Status == "Queued")
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(candidate => candidate.Status, "Generating")
                            .SetProperty(candidate => candidate.Progress, 10)
                            .SetProperty(candidate => candidate.StartedAt, DateTime.UtcNow), stoppingToken);
                    if (claimed == 0) continue;

                    var job = await db.MigrationJobs.SingleAsync(candidate => candidate.Id == candidateId, stoppingToken);
                    logger.LogInformation("Claimed migration job {JobId} for project {ProjectId}", job.Id, job.ProjectId);
                    var storageRoot = configuration["Storage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
                    var sourceRoot = Path.Combine(storageRoot, "uploads", job.ProjectId.ToString("N"));
                    var outputRoot = Path.Combine(storageRoot, "outputs", job.Id.ToString("N"));
                    var provider = providers.Resolve(job.SourceTechnology, job.TargetTechnology);
                    var result = provider is null
                        ? new("Failed", null, Array.Empty<string>(), Array.Empty<string>(), $"No provider supports {job.SourceTechnology} -> {job.TargetTechnology}.")
                        : await provider.ExecuteAsync(new MigrationRequest(job.Id, job.SourceTechnology, job.TargetTechnology, sourceRoot, outputRoot), stoppingToken);
                    job.Progress = 75;
                    job.Status = result.Status;
                    job.OutputStorageKey = result.OutputRoot is null ? null : job.Id.ToString("N");
                    job.ErrorMessage = result.ErrorMessage ?? string.Join(" ", result.Warnings);
                    if (result.Status != "Failed")
                    {
                        using var validationScope = scopeFactory.CreateScope();
                        var validation = validationScope.ServiceProvider.GetRequiredService<ValidationService>();
                        var validationResult = await validation.ValidateOutputAsync(job.Id, result.OutputRoot, stoppingToken);
                        if (validationResult.Status is "Unavailable" or "Failed") job.Status = "Failed";
                        else if (validationResult.Status == "ManualReview") job.Status = "ManualReview";
                    }
                    job.Progress = 100;
                    job.CompletedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Migration worker cycle failed."); }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
