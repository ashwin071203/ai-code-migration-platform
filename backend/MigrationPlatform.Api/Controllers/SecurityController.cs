using System.Security.Claims;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects/{projectId:guid}/security")]
public sealed class SecurityController(SecurityAnalysisService security) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Analyze(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await security.AnalyzeAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), projectId, cancellationToken);
        return result is null ? NotFound(new { message = "Project was not found in your workspace." }) : Ok(result);
    }
}