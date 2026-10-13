using MigrationPlatform.Api.DTOs;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Mvc;

namespace MigrationPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth, AuditService audit) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 || string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return BadRequest(new { message = "Name, valid email, and an 8-character password are required." });
        var result = await auth.RegisterAsync(request.FullName, request.Email, request.Password);
        if (!result.Success) return Conflict(new { message = result.Message });
        await audit.RecordAsync("Registration", result.User!.Id, "User", result.User.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(new AuthResponse(result.Token!, new UserResponse(result.User.Id, result.User.FullName, result.User.Email, result.User.Role)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await auth.LoginAsync(request.Email, request.Password);
        if (!result.Success) return Unauthorized(new { message = result.Message });
        await audit.RecordAsync("Login", result.User!.Id, "User", result.User.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
        return Ok(new AuthResponse(result.Token!, new UserResponse(result.User.Id, result.User.FullName, result.User.Email, result.User.Role)));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, IWebHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return BadRequest(new { message = "A valid email is required." });
        var result = await auth.RequestPasswordResetAsync(request.Email, environment.IsDevelopment());
        return Ok(new { message = result.Message, developmentToken = result.DevelopmentToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8) return BadRequest(new { message = "Email, reset token, and an 8-character password are required." });
        var result = await auth.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(new { message = result.Message });
    }
}