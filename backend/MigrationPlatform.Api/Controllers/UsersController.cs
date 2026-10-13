using MigrationPlatform.Api.DTOs;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/users")]
public sealed class UsersController(AdminService admins, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> List(CancellationToken cancellationToken) => Ok((await admins.ListUsersAsync(cancellationToken)).Select(ToResponse));

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<AdminUserResponse>> UpdateRole(Guid id, UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await admins.SetRoleAsync(id, request.Role, cancellationToken);
        if (user is null) return BadRequest(new { message = "User was not found or the role is invalid." });
        await audit.RecordAsync("UserRoleChange", UserId(), "User", id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(ToResponse(user));
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<ActionResult<AdminUserResponse>> Enable(Guid id, CancellationToken cancellationToken) => await SetEnabled(id, true, cancellationToken);

    [HttpPost("{id:guid}/disable")]
    public async Task<ActionResult<AdminUserResponse>> Disable(Guid id, CancellationToken cancellationToken) => await SetEnabled(id, false, cancellationToken);

    private async Task<ActionResult<AdminUserResponse>> SetEnabled(Guid id, bool enabled, CancellationToken cancellationToken)
    {
        var user = await admins.SetEnabledAsync(id, enabled, cancellationToken);
        if (user is null) return NotFound(new { message = "User was not found." });
        await audit.RecordAsync(enabled ? "UserEnabled" : "UserDisabled", UserId(), "User", id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(ToResponse(user));
    }

    private Guid UserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private static AdminUserResponse ToResponse(MigrationPlatform.Data.Entities.User user) => new(user.Id, user.FullName, user.Email, user.Role, user.IsEnabled, user.CreatedAt);
}