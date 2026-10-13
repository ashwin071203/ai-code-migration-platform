using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Services;

public sealed class MigrationService(MigrationDbContext db)
{
    public static readonly IReadOnlyList<(string Type, string Source, string Target, string Description)> Options =
    [
        ("Language", "Java", "C#", "Move Java application code to idiomatic C#."),
        ("Framework", "Spring Boot", "ASP.NET Core Web API", "Map Spring web and dependency-injection patterns to ASP.NET Core."),
        ("Platform", "React Web", "React Native", "Prepare web components for a mobile React Native target."),
        ("Modernization", ".NET Framework", "Modern .NET", "Plan framework and project-file modernization."),
    ];

    public Task<List<MigrationJob>> ListAsync(Guid userId) => db.MigrationJobs.AsNoTracking().Where(job => job.RequestedById == userId).OrderByDescending(job => job.CreatedAt).ToListAsync();
    public Task<MigrationJob?> GetAsync(Guid userId, Guid jobId) => db.MigrationJobs.AsNoTracking().SingleOrDefaultAsync(job => job.Id == jobId && job.RequestedById == userId);

    public async Task<MigrationJob?> CreateAsync(Guid userId, Guid projectId, string source, string target)
    {
        var ownsProject = await db.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == userId);
        if (!ownsProject) return null;
        var job = new MigrationJob { ProjectId = projectId, RequestedById = userId, SourceTechnology = source.Trim(), TargetTechnology = target.Trim(), Status = "Created" };
        db.MigrationJobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    public async Task<MigrationJob?> GeneratePlanAsync(Guid userId, Guid jobId)
    {
        var job = await db.MigrationJobs.SingleOrDefaultAsync(item => item.Id == jobId && item.RequestedById == userId);
        if (job is null) return null;
        job.PlanJson = System.Text.Json.JsonSerializer.Serialize(new { steps = new[] { "Create target project structure", "Map dependencies and configuration", "Transform source files", "Review manual migration items", "Run target validation" }, risks = new[] { "Compatibility must be verified against the source profile", "Generated code requires developer review before production use" } });
        job.PlanCreatedAt = DateTime.UtcNow;
        job.Status = "PlanReady";
        await db.SaveChangesAsync();
        return job;
    }

    public async Task<MigrationJob?> StartAsync(Guid userId, Guid jobId)
    {
        var job = await db.MigrationJobs.SingleOrDefaultAsync(item => item.Id == jobId && item.RequestedById == userId);
        if (job is null || job.Status != "PlanReady") return null;
        job.Status = "Queued";
        await db.SaveChangesAsync();
        return job;
    }

    public async Task<MigrationJob?> CancelAsync(Guid userId, Guid jobId)
    {
        var job = await db.MigrationJobs.SingleOrDefaultAsync(item => item.Id == jobId && item.RequestedById == userId);
        if (job is null || job.Status is "Completed" or "Failed" or "ManualReview" or "Cancelled") return null;
        job.Status = "Cancelled";
        job.ErrorMessage = "Cancelled by the user.";
        job.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return job;
    }

    public Task<string?> GetPlanAsync(Guid userId, Guid jobId) => db.MigrationJobs.AsNoTracking().Where(item => item.Id == jobId && item.RequestedById == userId).Select(item => item.PlanJson).SingleOrDefaultAsync();

    public Task<MigrationJob?> GetCompletedAsync(Guid userId, Guid jobId) => db.MigrationJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == jobId && item.RequestedById == userId && item.CompletedAt != null);
}