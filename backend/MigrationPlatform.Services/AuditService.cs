using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;

namespace MigrationPlatform.Services;

public sealed class AuditService(MigrationDbContext db)
{
    public async Task RecordAsync(string action, Guid? userId, string? entityType = null, string? entityId = null, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog { Action = action, UserId = userId, EntityType = entityType, EntityId = entityId, IpAddress = ipAddress });
        await db.SaveChangesAsync(cancellationToken);
    }
}