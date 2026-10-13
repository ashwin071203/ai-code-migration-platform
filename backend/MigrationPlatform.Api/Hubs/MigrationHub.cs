using System.Security.Claims;
using MigrationPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Api.Hubs;

[Authorize]
public sealed class MigrationHub(MigrationDbContext db) : Hub
{
    public async Task Subscribe(Guid migrationId)
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId) || !await db.MigrationJobs.AnyAsync(job => job.Id == migrationId && job.RequestedById == userId)) throw new HubException("Migration is not available in your workspace.");
        await Groups.AddToGroupAsync(Context.ConnectionId, migrationId.ToString());
    }

    public Task Unsubscribe(Guid migrationId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, migrationId.ToString());
}