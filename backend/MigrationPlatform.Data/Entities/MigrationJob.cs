namespace MigrationPlatform.Data.Entities;

public sealed class MigrationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid RequestedById { get; set; }
    public string SourceTechnology { get; set; } = string.Empty;
    public string TargetTechnology { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PlanJson { get; set; }
    public string? OutputStorageKey { get; set; }
    public DateTime? PlanCreatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}