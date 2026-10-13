using System.Security.Claims;
using MigrationPlatform.Api.DTOs;
using MigrationPlatform.Api.Hubs;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/migrations")]
public sealed class MigrationsController(MigrationService migrations, IConfiguration configuration, IHubContext<MigrationHub> hub, AuditService audit, ValidationService validation) : ControllerBase
{
    [HttpGet("options")]
    public ActionResult<IEnumerable<MigrationOption>> Options() => Ok(MigrationService.Options.Select(option => new MigrationOption(option.Type, option.Source, option.Target, option.Description)));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MigrationResponse>>> List() => Ok((await migrations.ListAsync(UserId())).Select(ToResponse));

    [HttpPost]
    public async Task<ActionResult<MigrationResponse>> Create(CreateMigrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceTechnology) || string.IsNullOrWhiteSpace(request.TargetTechnology)) return BadRequest(new { message = "Source and target technologies are required." });
        var job = await migrations.CreateAsync(UserId(), request.ProjectId, request.SourceTechnology, request.TargetTechnology);
        if (job is null) return NotFound(new { message = "Project was not found in your workspace." });
        await audit.RecordAsync("MigrationCreation", UserId(), "Migration", job.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(ToResponse(job));
    }

    [HttpPost("{id:guid}/plan")]
    public async Task<ActionResult<MigrationPlanResponse>> Plan(Guid id)
    {
        var job = await migrations.GeneratePlanAsync(UserId(), id);
        return job is null ? NotFound(new { message = "Migration was not found in your workspace." }) : Ok(ToPlanResponse(job));
    }

    [HttpGet("{id:guid}/plan")]
    public async Task<ActionResult<MigrationPlanResponse>> GetPlan(Guid id)
    {
        var job = await migrations.GetAsync(UserId(), id);
        var planJson = await migrations.GetPlanAsync(UserId(), id);
        return job is null || planJson is null ? NotFound(new { message = "No migration plan is available." }) : Ok(ToPlanResponse(job));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<MigrationResponse>> Start(Guid id)
    {
        var job = await migrations.StartAsync(UserId(), id);
        if (job is null) return BadRequest(new { message = "A reviewed migration plan is required before starting." });
        await audit.RecordAsync("MigrationStart", UserId(), "Migration", job.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        await hub.Clients.Group(job.Id.ToString()).SendAsync("MigrationUpdated", ToResponse(job));
        return Accepted(ToResponse(job));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<MigrationResponse>> Cancel(Guid id)
    {
        var job = await migrations.CancelAsync(UserId(), id);
        if (job is not null) await audit.RecordAsync("MigrationCancellation", UserId(), "Migration", job.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return job is null ? BadRequest(new { message = "This migration cannot be cancelled in its current state." }) : Ok(ToResponse(job));
    }

    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> Report(Guid id)
    {
        var job = await migrations.GetCompletedAsync(UserId(), id);
        return job is null ? NotFound(new { message = "Migration was not found in your workspace." }) : Ok(new { job.Id, job.Status, job.SourceTechnology, job.TargetTechnology, job.Progress, job.ErrorMessage, job.OutputStorageKey, job.StartedAt, job.CompletedAt });
    }

    [HttpGet("{id:guid}/validation")]
    public async Task<ActionResult<ValidationResponse>> Validation(Guid id, CancellationToken cancellationToken)
    {
        var result = await validation.GetAsync(UserId(), id, cancellationToken);
        return result is null ? NotFound(new { message = "Validation has not run for this migration." }) : Ok(new ValidationResponse(id, result.Status, result.FilesChecked, result.Errors, result.Warnings, result.Summary, result.CreatedAt));
    }

    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> Files(Guid id)
    {
        var job = await migrations.GetCompletedAsync(UserId(), id);
        if (job?.OutputStorageKey is null) return NotFound(new { message = "No generated output is available." });
        var root = OutputRoot(job.OutputStorageKey);
        if (!Directory.Exists(root)) return NotFound(new { message = "Generated output is not available." });
        return Ok(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/')).OrderBy(path => path));
    }

    [HttpGet("{id:guid}/files/{*path}")]
    public async Task<IActionResult> File(Guid id, string path, CancellationToken cancellationToken)
    {
        var job = await migrations.GetCompletedAsync(UserId(), id);
        if (job?.OutputStorageKey is null) return NotFound(new { message = "No generated output is available." });
        var root = OutputRoot(job.OutputStorageKey);
        var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
        var file = Path.GetFullPath(Path.Combine(root, normalizedPath));
        if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(file)) return NotFound(new { message = "Generated file was not found." });
        return Ok(new { path, content = await System.IO.File.ReadAllTextAsync(file, cancellationToken) });
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var job = await migrations.GetCompletedAsync(UserId(), id);
        if (job is null || job.OutputStorageKey is null) return NotFound(new { message = "No generated output is available for this migration." });
        var root = configuration["Storage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        var output = Path.GetFullPath(Path.Combine(root, "outputs", job.OutputStorageKey));
        var outputRoot = Path.GetFullPath(Path.Combine(root, "outputs"));
        if (!output.StartsWith(outputRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(output)) return NotFound(new { message = "Generated output is not available." });
        await using var archive = new MemoryStream();
        System.IO.Compression.ZipFile.CreateFromDirectory(output, archive);
        return File(archive.ToArray(), "application/zip", $"migration-{job.Id:N}.zip");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MigrationResponse>> Get(Guid id)
    {
        var job = await migrations.GetAsync(UserId(), id);
        return job is null ? NotFound(new { message = "Migration was not found in your workspace." }) : Ok(ToResponse(job));
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string OutputRoot(string outputKey)
    {
        var storageRoot = configuration["Storage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        return Path.GetFullPath(Path.Combine(storageRoot, "outputs", outputKey));
    }
    private static MigrationResponse ToResponse(MigrationPlatform.Data.Entities.MigrationJob job) => new(job.Id, job.ProjectId, job.SourceTechnology, job.TargetTechnology, job.Status, job.Progress, job.ErrorMessage, job.OutputStorageKey is not null, job.CreatedAt);
    private static MigrationPlanResponse ToPlanResponse(MigrationPlatform.Data.Entities.MigrationJob job) => new(job.Id, job.SourceTechnology, job.TargetTechnology, job.Status, new[] { "Create target project structure", "Map dependencies and configuration", "Transform source files", "Review manual migration items", "Run target validation" }, new[] { "Compatibility must be verified against the source profile", "Generated code requires developer review before production use" }, job.PlanCreatedAt ?? job.CreatedAt);
}