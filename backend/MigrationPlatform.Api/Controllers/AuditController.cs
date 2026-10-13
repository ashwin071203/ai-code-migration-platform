using MigrationPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/audit")]
public sealed class AuditController(MigrationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(log => log.CreatedAt).Take(200).Select(log => new { log.Id, log.UserId, log.Action, log.EntityType, log.EntityId, log.IpAddress, log.CreatedAt }).ToListAsync(cancellationToken));
}