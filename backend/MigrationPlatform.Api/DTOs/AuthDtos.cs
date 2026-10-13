namespace MigrationPlatform.Api.DTOs;

public sealed record RegisterRequest(string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, UserResponse User);
public sealed record UserResponse(Guid Id, string FullName, string Email, string Role);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);