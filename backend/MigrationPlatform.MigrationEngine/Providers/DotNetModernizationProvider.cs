using MigrationPlatform.MigrationEngine.Interfaces;
using MigrationPlatform.MigrationEngine.Models;

namespace MigrationPlatform.MigrationEngine.Providers;

public sealed class DotNetModernizationProvider : IMigrationProvider
{
    public bool CanHandle(string sourceTechnology, string targetTechnology) =>
        sourceTechnology.Equals(".NET Framework", StringComparison.OrdinalIgnoreCase) && targetTechnology.Equals("Modern .NET", StringComparison.OrdinalIgnoreCase);

    public async Task<MigrationResult> ExecuteAsync(MigrationRequest request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.SourceRoot)) return new("Failed", null, [], [], "The source workspace does not exist.");
        Directory.CreateDirectory(request.OutputRoot);
        var files = new List<string>();
        foreach (var sourceFile in Directory.EnumerateFiles(request.SourceRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(request.SourceRoot, sourceFile);
            var destination = Path.Combine(request.OutputRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var input = File.OpenRead(sourceFile);
            await using var output = File.Create(destination);
            await input.CopyToAsync(output, cancellationToken);
            files.Add(relative);
        }
        await File.WriteAllTextAsync(Path.Combine(request.OutputRoot, "FORGESHIFT-MANUAL-REVIEW.md"), "# Manual review required\n\nThe source workspace was copied for review. Project-file, API, dependency, and validation changes must be completed by the migration engine in a later provider revision.\n", cancellationToken);
        return new("ManualReview", request.OutputRoot, ["Source files were copied without transformation.", "Project and dependency modernization requires manual review."], files, null);
    }
}