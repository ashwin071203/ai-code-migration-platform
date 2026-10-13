namespace MigrationPlatform.Data.Entities;

public sealed class ValidationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MigrationJobId { get; set; }
    public MigrationJob? MigrationJob { get; set; }
    public string Status { get; set; } = "NotRun";
    public int FilesChecked { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}