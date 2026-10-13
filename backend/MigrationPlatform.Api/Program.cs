using System.Text;
using MigrationPlatform.Data;
using MigrationPlatform.Api.Hubs;
using MigrationPlatform.Api.Middleware;
using MigrationPlatform.AI.Implementations;
using MigrationPlatform.AI.Interfaces;
using MigrationPlatform.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddDbContext<MigrationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<MigrationService>();
builder.Services.AddScoped<ProjectAnalysisService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<RuleService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<SecurityAnalysisService>();
builder.Services.AddHttpClient<OllamaAIProvider>();
builder.Services.AddHttpClient<OpenAICompatibleProvider>();
builder.Services.AddSingleton<DevelopmentFallbackProvider>();
builder.Services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
builder.Services.AddHostedService<MigrationStatusBroadcaster>();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key must be configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/migrations")) context.Token = accessToken;
            return Task.CompletedTask;
        },
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

var bootstrapAdminEmail = app.Configuration["Bootstrap:AdminEmail"];
var bootstrapAdminPassword = app.Configuration["Bootstrap:AdminPassword"];
if (!string.IsNullOrWhiteSpace(bootstrapAdminEmail) || !string.IsNullOrWhiteSpace(bootstrapAdminPassword))
{
    if (string.IsNullOrWhiteSpace(bootstrapAdminEmail) || string.IsNullOrWhiteSpace(bootstrapAdminPassword)) throw new InvalidOperationException("Bootstrap admin email and password must be configured together.");
    using var bootstrapScope = app.Services.CreateScope();
    await bootstrapScope.ServiceProvider.GetRequiredService<AuthService>().EnsureAdminAsync(bootstrapAdminEmail, bootstrapAdminPassword, CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MigrationHub>("/hubs/migrations");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MigrationPlatform.Api" }));
app.MapGet("/health/database", async (MigrationDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ok", database = "reachable" })
            : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
