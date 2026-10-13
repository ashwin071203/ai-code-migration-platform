using System.Security.Claims;
using MigrationPlatform.Api.DTOs;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects")]
public sealed class ProjectsController(ProjectService projects, ProjectAnalysisService analysis, AuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> List() => Ok((await projects.ListAsync(UserId())).Select(ToResponse));

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Project name is required." });
        var project = await projects.CreateAsync(UserId(), request.Name, request.Description);
        await audit.RecordAsync("ProjectCreation", UserId(), "Project", project.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(ToResponse(project));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailResponse>> Get(Guid id)
    {
        var project = await projects.GetAsync(UserId(), id);
        return project is null || project.Status == "Deleted" ? NotFound(new { message = "Project was not found in your workspace." }) : Ok(ToDetailResponse(project));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await projects.DeleteAsync(UserId(), id);
        if (!deleted) return NotFound(new { message = "Project was not found in your workspace." });
        await audit.RecordAsync("ProjectDeletion", UserId(), "Project", id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    [HttpPost("{id:guid}/upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid id, IFormFile archive, CancellationToken cancellationToken)
    {
        if (archive is null) return BadRequest(new { message = "A ZIP archive is required." });
        var result = await analysis.UploadAndAnalyzeAsync(UserId(), id, archive.OpenReadStream(), archive.FileName, archive.Length, cancellationToken);
        if (result.Success) await audit.RecordAsync("ProjectUpload", UserId(), "Project", id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? Ok(new { message = result.Message, profile = ToProfileResponse(result.Profile!) }) : BadRequest(new { message = result.Message });
    }

    [HttpGet("{id:guid}/analysis")]
    public async Task<ActionResult<SourceProfileResponse>> Analysis(Guid id, CancellationToken cancellationToken)
    {
        var profile = await analysis.GetAsync(UserId(), id, cancellationToken);
        return profile is null ? NotFound(new { message = "No analysis is available for this project." }) : Ok(ToProfileResponse(profile));
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static ProjectResponse ToResponse(MigrationPlatform.Data.Entities.Project project) => new(project.Id, project.Name, project.Description, project.SourceLanguage, project.Framework, project.Status, project.CreatedAt);
    private static ProjectDetailResponse ToDetailResponse(MigrationPlatform.Data.Entities.Project project) => new(project.Id, project.Name, project.Description, project.SourceLanguage, project.Framework, project.Status, project.FileCount, project.AnalysisJson is not null, project.CreatedAt, project.UpdatedAt);
    private static SourceProfileResponse ToProfileResponse(SourceProfile profile) => new(profile.Language, profile.LanguageVersion, profile.Framework, profile.FrameworkVersion, profile.ProjectType, profile.BuildTool, profile.Files, profile.Dependencies, profile.Evidence);
}