using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Services;

public sealed class ProjectService(MigrationDbContext db)
{
    public Task<List<Project>> ListAsync(Guid ownerId) => db.Projects.AsNoTracking().Where(project => project.OwnerId == ownerId && project.Status != "Deleted").OrderByDescending(project => project.UpdatedAt).ToListAsync();
    public Task<Project?> GetAsync(Guid ownerId, Guid projectId) => db.Projects.AsNoTracking().SingleOrDefaultAsync(project => project.OwnerId == ownerId && project.Id == projectId);
    public async Task<Project> CreateAsync(Guid ownerId, string name, string description)
    {
        var project = new Project { OwnerId = ownerId, Name = name.Trim(), Description = description.Trim() };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid projectId)
    {
        var project = await db.Projects.SingleOrDefaultAsync(item => item.OwnerId == ownerId && item.Id == projectId);
        if (project is null) return false;
        project.Status = "Deleted";
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }
}