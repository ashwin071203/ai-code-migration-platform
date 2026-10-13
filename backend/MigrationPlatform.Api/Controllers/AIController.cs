using MigrationPlatform.AI.Interfaces;
using MigrationPlatform.AI.Models;
using MigrationPlatform.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/ai")]
public sealed class AIController(IAIProviderFactory providers) : ControllerBase
{
    [HttpPost("complete")]
    public async Task<ActionResult<AIResponse>> Complete(AICompletionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt)) return BadRequest(new { message = "A prompt is required." });
        if (request.Prompt.Length > 100_000) return BadRequest(new { message = "The prompt cannot exceed 100,000 characters." });
        if (request.SystemPrompt?.Length > 20_000) return BadRequest(new { message = "The system prompt cannot exceed 20,000 characters." });
        if (request.Model?.Length > 200) return BadRequest(new { message = "The model name cannot exceed 200 characters." });

        try
        {
            var completion = await providers.Resolve().CompleteAsync(
                new AIRequest(request.SystemPrompt ?? "You assist with software migration tasks.", request.Prompt, request.Model),
                cancellationToken);
            return Ok(completion);
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                code = "AI_PROVIDER_UNAVAILABLE",
                message = "The configured AI provider could not complete the request. Start Ollama and pull the configured model, or update AI:Provider and AI:Endpoint.",
                detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                code = "AI_CONFIGURATION_INVALID",
                message = exception.Message
            });
        }
    }
}