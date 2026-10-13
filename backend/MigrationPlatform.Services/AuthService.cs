using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MigrationPlatform.Services;

public sealed class AuthService(MigrationDbContext db, IConfiguration configuration)
{
    public async Task EnsureAdminAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || password.Length < 12) throw new InvalidOperationException("Bootstrap admin credentials must include an email and a password of at least 12 characters.");
        if (await db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken)) return;

        db.Users.Add(new User
        {
            FullName = "Platform Administrator",
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(bool Success, string Message, User? User, string? Token)> RegisterAsync(string name, string email, string password)
    {
        email = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email)) return (false, "An account with that email already exists.", null, null);
        var user = new User { FullName = name.Trim(), Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, "Account created.", user, CreateToken(user));
    }

    public async Task<(bool Success, string Message, User? User, string? Token)> LoginAsync(string email, string password)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email.Trim().ToLowerInvariant());
        if (user is null || !user.IsEnabled || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return (false, "Invalid credentials.", null, null);
        return (true, "Signed in.", user, CreateToken(user));
    }

    public async Task<(string Message, string? DevelopmentToken)> RequestPasswordResetAsync(string email, bool exposeDevelopmentToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email.Trim().ToLowerInvariant());
        if (user is null) return ("If an account exists, reset instructions have been created.", null);
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        db.PasswordResetRequests.Add(new PasswordResetRequest { UserId = user.Id, TokenHash = HashToken(rawToken), ExpiresAt = DateTime.UtcNow.AddMinutes(30) });
        await db.SaveChangesAsync();
        return ("If an account exists, reset instructions have been created.", exposeDevelopmentToken ? rawToken : null);
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string rawToken, string newPassword)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var request = await db.PasswordResetRequests.Include(item => item.User).SingleOrDefaultAsync(item => item.User!.Email == normalizedEmail && item.UsedAt == null && item.ExpiresAt > DateTime.UtcNow && item.TokenHash == HashToken(rawToken));
        if (request?.User is null) return (false, "The reset token is invalid or expired.");
        request.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        request.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return (true, "Password reset successfully.");
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string CreateToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Role, user.Role), new Claim(ClaimTypes.Email, user.Email) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims: claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: credentials));
    }
}