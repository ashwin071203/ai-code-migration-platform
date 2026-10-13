using MigrationPlatform.Api.DTOs;
using MigrationPlatform.Data.Entities;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/rules")]
public sealed class RulesController(RuleService rules, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MigrationRuleResponse>>> List(CancellationToken cancellationToken) => Ok((await rules.ListAsync(cancellationToken)).Select(ToResponse));

    [HttpPost]
    public async Task<ActionResult<MigrationRuleResponse>> Create(MigrationRuleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.SourceTechnology) || string.IsNullOrWhiteSpace(request.TargetTechnology)) return BadRequest(new { message = "Name, source, and target technology are required." });
        var rule = await rules.CreateAsync(new MigrationRule { Name = request.Name.Trim(), SourceTechnology = request.SourceTechnology.Trim(), TargetTechnology = request.TargetTechnology.Trim(), Pattern = request.Pattern, Replacement = request.Replacement, Description = request.Description, Priority = request.Priority, Enabled = request.Enabled }, cancellationToken);
        await audit.RecordAsync("RuleCreation", UserId(), "MigrationRule", rule.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Created($"/api/v1/rules/{rule.Id}", ToResponse(rule));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MigrationRuleResponse>> Update(Guid id, MigrationRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await rules.UpdateAsync(id, new MigrationRuleRequestValues(request.Name, request.SourceTechnology, request.TargetTechnology, request.Pattern, request.Replacement, request.Description, request.Priority, request.Enabled), cancellationToken);
        return rule is null ? NotFound(new { message = "Migration rule was not found." }) : Ok(ToResponse(rule));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await rules.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(new { message = "Migration rule was not found." });
        await audit.RecordAsync("RuleDeletion", UserId(), "MigrationRule", id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return NoContent();
    }

    private Guid UserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    private static MigrationRuleResponse ToResponse(MigrationRule rule) => new(rule.Id, rule.Name, rule.SourceTechnology, rule.TargetTechnology, rule.Pattern, rule.Replacement, rule.Description, rule.Priority, rule.Enabled, rule.UpdatedAt);
}