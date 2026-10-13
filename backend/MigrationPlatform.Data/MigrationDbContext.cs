using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Data;

public sealed class MigrationDbContext(DbContextOptions<MigrationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<MigrationJob> MigrationJobs => Set<MigrationJob>();
    public DbSet<ProjectFile> ProjectFiles => Set<ProjectFile>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MigrationRule> MigrationRules => Set<MigrationRule>();
    public DbSet<ValidationResult> ValidationResults => Set<ValidationResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(user => user.Email).IsUnique();
        modelBuilder.Entity<Project>().HasIndex(project => project.OwnerId);
        modelBuilder.Entity<Project>().HasOne(project => project.Owner).WithMany(user => user.Projects).HasForeignKey(project => project.OwnerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MigrationJob>().HasIndex(job => new { job.Status, job.CreatedAt });
        modelBuilder.Entity<MigrationJob>().HasOne(job => job.Project).WithMany().HasForeignKey(job => job.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProjectFile>().HasIndex(file => new { file.ProjectId, file.RelativePath }).IsUnique();
        modelBuilder.Entity<ProjectFile>().HasOne(file => file.Project).WithMany(project => project.Files).HasForeignKey(file => file.ProjectId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PasswordResetRequest>().HasIndex(request => request.TokenHash).IsUnique();
        modelBuilder.Entity<PasswordResetRequest>().HasOne(request => request.User).WithMany().HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AuditLog>().HasIndex(log => new { log.CreatedAt, log.Action });
        modelBuilder.Entity<AuditLog>().HasIndex(log => log.UserId);
        modelBuilder.Entity<MigrationRule>().HasIndex(rule => new { rule.SourceTechnology, rule.TargetTechnology, rule.Enabled });
        modelBuilder.Entity<ValidationResult>().HasIndex(result => result.MigrationJobId).IsUnique();
        modelBuilder.Entity<ValidationResult>().HasOne(result => result.MigrationJob).WithMany().HasForeignKey(result => result.MigrationJobId).OnDelete(DeleteBehavior.Cascade);
    }
}