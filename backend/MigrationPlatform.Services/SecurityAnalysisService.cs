using System.Text.RegularExpressions;
using MigrationPlatform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MigrationPlatform.Services;

public sealed record SecurityFinding(string Rule, string Severity, string RelativePath, int Line, string Evidence);
public sealed record SecurityAnalysis(string Status, int FilesScanned, int Findings, IReadOnlyList<SecurityFinding> Results);

public sealed class SecurityAnalysisService(MigrationDbContext db, IConfiguration configuration)
{
    private static readonly (string Rule, string Severity, Regex Pattern)[] Rules =
    [
        ("PrivateKey", "Critical", new Regex("-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("CloudAccessKey", "High", new Regex("\\bAKIA[0-9A-Z]{16}\\b", RegexOptions.Compiled)),
        ("HardcodedSecret", "High", new Regex("(?i)(password|passwd|secret|api[_-]?key)\\s*[:=]\\s*[\\\"']?[^\\s\\\"']{8,}" , RegexOptions.Compiled)),
        ("InsecureHttp", "Medium", new Regex("https?://", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
        ("DynamicEvaluation", "High", new Regex("(?i)\\b(eval|exec)\\s*\\(", RegexOptions.Compiled))
    ];

    public async Task<SecurityAnalysis?> AnalyzeAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        var ownsProject = await db.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == userId, cancellationToken);
        if (!ownsProject) return null;
        var root = configuration["Storage:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        var projectRoot = Path.GetFullPath(Path.Combine(root, "uploads", projectId.ToString("N")));
        if (!Directory.Exists(projectRoot)) return new("Unavailable", 0, 0, []);

        var findings = new List<SecurityFinding>();
        var files = Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories).Where(IsTextFile).ToArray();
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = await File.ReadAllLinesAsync(file, cancellationToken);
            for (var index = 0; index < lines.Length; index++)
                foreach (var rule in Rules.Where(rule => rule.Pattern.IsMatch(lines[index])))
                    findings.Add(new(rule.Rule, rule.Severity, Path.GetRelativePath(projectRoot, file), index + 1, Redact(lines[index])));
        }
        return new("Completed", files.Length, findings.Count, findings);
    }

    private static bool IsTextFile(string path) => Path.GetExtension(path).ToLowerInvariant() is ".cs" or ".java" or ".js" or ".jsx" or ".ts" or ".tsx" or ".json" or ".xml" or ".yml" or ".yaml" or ".properties" or ".config";
    private static string Redact(string line) => Regex.Replace(line.Trim(), "(?i)(password|passwd|secret|api[_-]?key)(\\s*[:=]\\s*)[^\\s,;]+", "$1$2[REDACTED]");
}