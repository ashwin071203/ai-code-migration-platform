namespace MigrationPlatform.Data.Entities;

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "Not analyzed";
    public string Framework { get; set; } = "Not analyzed";
    public string Status { get; set; } = "Created";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int FileCount { get; set; }
    public string? StorageKey { get; set; }
    public string? AnalysisJson { get; set; }
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
}