using System.IO.Compression;
using System.Text.Json;
using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MigrationPlatform.Services;

public sealed record SourceProfile(string Language, string LanguageVersion, string Framework, string FrameworkVersion, string ProjectType, string BuildTool, int Files, int Dependencies, IReadOnlyList<string> Evidence);

public sealed class ProjectAnalysisService(MigrationDbContext db, IConfiguration configuration)
{
    private const long MaxArchiveBytes = 100 * 1024 * 1024;
    private const long MaxExpandedBytes = 500 * 1024 * 1024;
    private const int MaxFiles = 20_000;

    public async Task<(bool Success, string Message, SourceProfile? Profile)> UploadAndAnalyzeAsync(Guid ownerId, Guid projectId, Stream archive, string fileName, long fileLength, CancellationToken cancellationToken)
    {
        var project = await db.Projects.Include(item => item.Files).SingleOrDefaultAsync(item => item.Id == projectId && item.OwnerId == ownerId, cancellationToken);
        if (project is null) return (false, "Project was not found in your workspace.", null);
        if (fileLength == 0 || fileLength > MaxArchiveBytes || !Path.GetFileName(fileName).EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return (false, "Upload a non-empty ZIP file up to 100 MB.", null);

        var root = configuration["Storage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        var projectRoot = Path.GetFullPath(Path.Combine(root, "uploads", projectId.ToString("N")));
        Directory.CreateDirectory(projectRoot);
        foreach (var file in project.Files) db.ProjectFiles.Remove(file);
        foreach (var existing in Directory.EnumerateFileSystemEntries(projectRoot))
        {
            if (Directory.Exists(existing)) Directory.Delete(existing, true);
            else File.Delete(existing);
        }

        var extractedBytes = 0L;
        var fileCount = 0;
        var evidence = new List<string>();
        await using var input = archive;
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(input, ZipArchiveMode.Read);
        }
        catch (InvalidDataException)
        {
            return (false, "The uploaded file is not a valid ZIP archive.", null);
        }
        using (zip)
        {
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            if (++fileCount > MaxFiles) return (false, "The archive contains too many files.", null);
            var relativePath = entry.FullName.Replace('\\', '/').TrimStart('/');
            var destination = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return (false, "The archive contains an unsafe path.", null);
            extractedBytes += entry.Length;
            if (extractedBytes > MaxExpandedBytes) return (false, "The expanded archive exceeds the 500 MB safety limit.", null);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using (var output = File.Create(destination)) await entry.Open().CopyToAsync(output, cancellationToken);
            db.ProjectFiles.Add(new ProjectFile { ProjectId = projectId, RelativePath = relativePath, SizeBytes = entry.Length });
            AddEvidence(relativePath, evidence);
        }
        }

        var profile = BuildProfile(evidence, fileCount);
        project.FileCount = fileCount;
        project.StorageKey = projectId.ToString("N");
        project.SourceLanguage = profile.Language;
        project.Framework = profile.Framework;
        project.AnalysisJson = JsonSerializer.Serialize(profile);
        project.Status = "AnalysisCompleted";
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (true, "Project analyzed.", profile);
    }

    public async Task<SourceProfile?> GetAsync(Guid ownerId, Guid projectId, CancellationToken cancellationToken)
    {
        var json = await db.Projects.AsNoTracking().Where(project => project.Id == projectId && project.OwnerId == ownerId).Select(project => project.AnalysisJson).SingleOrDefaultAsync(cancellationToken);
        return json is null ? null : JsonSerializer.Deserialize<SourceProfile>(json);
    }

    private static void AddEvidence(string path, ICollection<string> evidence)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        if (name is "pom.xml" or "build.gradle" or "build.gradle.kts" or "package.json") evidence.Add(path);
        else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)) evidence.Add(path);
    }

    private static SourceProfile BuildProfile(IReadOnlyCollection<string> evidence, int files)
    {
        var java = evidence.Any(path => path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase) || path.EndsWith("build.gradle", StringComparison.OrdinalIgnoreCase));
        var dotnet = evidence.Any(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));
        var node = evidence.Any(path => path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));
        var language = java ? "Java" : dotnet ? "C#" : node ? "JavaScript/TypeScript" : "Unknown";
        var framework = java ? (evidence.Any(path => path.Contains("spring", StringComparison.OrdinalIgnoreCase)) ? "Spring Boot" : "Java") : dotnet ? ".NET" : node ? "Node.js" : "Unknown";
        var buildTool = java ? (evidence.Any(path => path.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase)) ? "Maven" : "Gradle") : dotnet ? "MSBuild" : node ? "npm" : "Unknown";
        return new SourceProfile(language, "Detected from manifest", framework, "Detected from manifest", "Unknown", buildTool, files, evidence.Count, evidence.Take(20).ToArray());
    }
}