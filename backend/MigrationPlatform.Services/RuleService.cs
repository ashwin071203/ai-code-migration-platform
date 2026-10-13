using MigrationPlatform.Data;
using MigrationPlatform.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MigrationPlatform.Services;

public sealed class RuleService(MigrationDbContext db)
{
    public Task<List<MigrationRule>> ListAsync(CancellationToken cancellationToken) => db.MigrationRules.AsNoTracking().OrderBy(rule => rule.Priority).ThenBy(rule => rule.Name).ToListAsync(cancellationToken);

    public async Task<MigrationRule> CreateAsync(MigrationRule rule, CancellationToken cancellationToken)
    {
        db.MigrationRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<MigrationRule?> UpdateAsync(Guid id, MigrationRuleRequestValues values, CancellationToken cancellationToken)
    {
        var rule = await db.MigrationRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null) return null;
        values.Apply(rule);
        await db.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await db.MigrationRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null) return false;
        db.MigrationRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record MigrationRuleRequestValues(string Name, string SourceTechnology, string TargetTechnology, string Pattern, string Replacement, string Description, int Priority, bool Enabled)
{
    public void Apply(MigrationRule rule)
    {
        rule.Name = Name.Trim(); rule.SourceTechnology = SourceTechnology.Trim(); rule.TargetTechnology = TargetTechnology.Trim(); rule.Pattern = Pattern; rule.Replacement = Replacement; rule.Description = Description; rule.Priority = Priority; rule.Enabled = Enabled; rule.UpdatedAt = DateTime.UtcNow;
    }
}