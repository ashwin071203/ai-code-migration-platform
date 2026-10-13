using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace MigrationPlatform.Services;

public sealed class ValidationService(MigrationDbContext db)
{
    public async Task<ValidationResult> ValidateOutputAsync(Guid migrationId, string? outputRoot, CancellationToken cancellationToken)
    {
        var result = new ValidationResult { MigrationJobId = migrationId };
        if (string.IsNullOrWhiteSpace(outputRoot) || !Directory.Exists(outputRoot))
        {
            result.Status = "Unavailable";
            result.Errors = 1;
            result.Summary = "Generated output directory is unavailable.";
        }
        else
        {
            var files = Directory.EnumerateFiles(outputRoot, "*", SearchOption.AllDirectories).ToArray();
            result.FilesChecked = files.Length;
            var manualReview = files.Any(file => Path.GetFileName(file).Equals("FORGESHIFT-MANUAL-REVIEW.md", StringComparison.OrdinalIgnoreCase));
            var emptyFiles = files.Count(file => new FileInfo(file).Length == 0);
            var unconvertedFiles = await FindUnconvertedJavaFilesAsync(files, cancellationToken);
            result.Status = files.Length == 0 || emptyFiles > 0 || unconvertedFiles > 0 ? "Failed" : manualReview ? "ManualReview" : "Passed";
            result.Errors = (files.Length == 0 ? 1 : 0) + (emptyFiles > 0 ? 1 : 0) + (unconvertedFiles > 0 ? 1 : 0);
            result.Warnings = manualReview ? 1 : 0;
            result.Summary = result.Status switch
            {
                "Failed" when unconvertedFiles > 0 => $"Validation found unconverted Java syntax in {unconvertedFiles} generated source file(s).",
                "Failed" when emptyFiles > 0 => $"Validation found {emptyFiles} empty generated file(s).",
                "Failed" => "Generated output contains no files.",
                "ManualReview" => "Output exists, but the provider marked it for manual review.",
                _ => $"Validated {files.Length} generated files."
            };
        }
        var previous = await db.ValidationResults.SingleOrDefaultAsync(item => item.MigrationJobId == migrationId, cancellationToken);
        if (previous is null) db.ValidationResults.Add(result);
        else { previous.Status = result.Status; previous.FilesChecked = result.FilesChecked; previous.Errors = result.Errors; previous.Warnings = result.Warnings; previous.Summary = result.Summary; previous.CreatedAt = DateTime.UtcNow; result = previous; }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<int> FindUnconvertedJavaFilesAsync(IEnumerable<string> files, CancellationToken cancellationToken)
    {
        var sourceFiles = files.Where(file => Path.GetExtension(file) is ".cs" or ".java" or ".ts" or ".tsx" or ".jsx");
        var count = 0;
        foreach (var file in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, cancellationToken);
            if (Regex.IsMatch(content, @"^\s*(package|import)\s+[^;]+;", RegexOptions.Multiline) || content.Contains("public static void main(", StringComparison.Ordinal) || Regex.IsMatch(content, @"\bboolean\b")) count++;
        }
        return count;
    }

    public Task<ValidationResult?> GetAsync(Guid userId, Guid migrationId, CancellationToken cancellationToken) => db.ValidationResults.AsNoTracking().SingleOrDefaultAsync(result => result.MigrationJobId == migrationId && result.MigrationJob!.RequestedById == userId, cancellationToken);
}