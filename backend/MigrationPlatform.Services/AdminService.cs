using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Services;

public sealed class AdminService(MigrationDbContext db)
{
    public Task<List<User>> ListUsersAsync(CancellationToken cancellationToken) => db.Users.AsNoTracking().OrderBy(user => user.FullName).ToListAsync(cancellationToken);

    public async Task<User?> SetRoleAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        if (role is not ("Admin" or "Developer" or "Viewer")) return null;
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return null;
        user.Role = role;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> SetEnabledAsync(Guid userId, bool enabled, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return null;
        user.IsEnabled = enabled;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }
}