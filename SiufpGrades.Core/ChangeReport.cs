namespace SiufpGrades.Core;

/// <summary>Turns grade changes into human-readable notification lines.</summary>
public static class ChangeReport
{
    public static IReadOnlyList<string> Format<T>(string category, GradeChanges<T> changes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentNullException.ThrowIfNull(changes);

        if (changes.IsFirstRun)
        {
            return [$"{category}: first run, stored {changes.Added.Count} grade(s) as baseline."];
        }

        if (!changes.HasChanges)
        {
            return [$"{category}: nothing new."];
        }

        var lines = new List<string>
        {
            $"{category}: changes detected! ({changes.Added.Count} new/updated, {changes.Removed.Count} removed/replaced)",
        };
        lines.AddRange(changes.Added.Select(g => $"  + {g}"));
        lines.AddRange(changes.Removed.Select(g => $"  - {g}"));
        return lines;
    }
}
