using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using MigrationPlatform.MigrationEngine.Implementations;
using MigrationPlatform.MigrationEngine.Models;
using MigrationPlatform.MigrationEngine.Providers;
using MigrationPlatform.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MigrationPlatform.Tests;

public sealed class MigrationWorkflowTests
{
    [Fact]
    public void Factory_resolves_supported_provider_pairs()
    {
        var factory = new MigrationProviderFactory();

        Assert.IsType<JavaToCSharpProvider>(factory.Resolve("Java", "C#"));
        Assert.NotNull(factory.Resolve("Spring Boot", "ASP.NET Core Web API"));
        Assert.NotNull(factory.Resolve("React Web", "React Native"));
        Assert.Null(factory.Resolve("Python", "C#"));
    }

    [Fact]
    public async Task Java_provider_transforms_source_and_preserves_assets()
    {
        var root = Path.Combine(Path.GetTempPath(), "forgeshift-tests", Guid.NewGuid().ToString("N"));
        var sourceRoot = Path.Combine(root, "source");
        var outputRoot = Path.Combine(root, "output");
        Directory.CreateDirectory(sourceRoot);
        await File.WriteAllTextAsync(Path.Combine(sourceRoot, "Main.java"), "package demo;\nimport java.util.List;\npublic class Main { boolean ready = true; }");
        await File.WriteAllTextAsync(Path.Combine(sourceRoot, "README.md"), "review me");

        try
        {
            var result = await new JavaToCSharpProvider().ExecuteAsync(new MigrationRequest(Guid.NewGuid(), "Java", "C#", sourceRoot, outputRoot), CancellationToken.None);

            Assert.Equal("ManualReview", result.Status);
            Assert.Contains("Main.cs", result.FilesProcessed);
            Assert.True(File.Exists(Path.Combine(outputRoot, "README.md")));
            var generated = await File.ReadAllTextAsync(Path.Combine(outputRoot, "Main.cs"));
            Assert.Contains("bool ready", generated);
            Assert.True(File.Exists(Path.Combine(outputRoot, "FORGESHIFT-MANUAL-REVIEW.md")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Migration_service_enforces_project_ownership_and_state_transitions()
    {
        await using var db = new MigrationDbContext(new DbContextOptionsBuilder<MigrationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var owner = new User { FullName = "Owner", Email = "owner@example.com", PasswordHash = "hash" };
        var other = new User { FullName = "Other", Email = "other@example.com", PasswordHash = "hash" };
        var project = new Project { Name = "Demo", OwnerId = owner.Id };
        db.Users.AddRange(owner, other);
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        var service = new MigrationService(db);

        Assert.Null(await service.CreateAsync(other.Id, project.Id, "Java", "C#"));
        var job = await service.CreateAsync(owner.Id, project.Id, "Java", "C#");
        Assert.NotNull(job);
        Assert.Null(await service.StartAsync(owner.Id, job!.Id));
        Assert.NotNull(await service.GeneratePlanAsync(owner.Id, job.Id));
        Assert.NotNull(await service.StartAsync(owner.Id, job.Id));
        Assert.Equal("Queued", (await service.GetAsync(owner.Id, job.Id))!.Status);
    }
}
