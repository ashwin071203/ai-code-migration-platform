namespace MigrationPlatform.Api.DTOs;

public sealed record AdminUserResponse(Guid Id, string FullName, string Email, string Role, bool IsEnabled, DateTime CreatedAt);
public sealed record UpdateUserRoleRequest(string Role);