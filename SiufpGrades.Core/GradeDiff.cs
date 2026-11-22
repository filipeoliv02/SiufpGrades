namespace SiufpGrades.Core;

/// <summary>Result of comparing a freshly scraped grade list with the previously stored one.</summary>
public sealed record GradeChanges<T>(IReadOnlyList<T> Added, IReadOnlyList<T> Removed, bool IsFirstRun)
{
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0;
}

public static class GradeDiff
{
    /// <summary>
    /// Multiset comparison: order-insensitive, duplicates counted. A grade whose value changed
    /// shows up as one removed (old value) and one added (new value) entry.
    /// </summary>
    /// <param name="previous">Stored grades, or <c>null</c> when nothing was stored yet.</param>
    public static GradeChanges<T> Compare<T>(IReadOnlyList<T>? previous, IReadOnlyList<T> current)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(current);
        if (previous is null)
        {
            return new GradeChanges<T>(current.ToList(), [], IsFirstRun: true);
        }

        var remaining = previous.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());
        var added = new List<T>();
        foreach (var grade in current)
        {
            if (remaining.TryGetValue(grade, out var count) && count > 0)
            {
                remaining[grade] = count - 1;
            }
            else
            {
                added.Add(grade);
            }
        }

        var removed = previous
            .Distinct()
            .SelectMany(g => Enumerable.Repeat(g, remaining.GetValueOrDefault(g)))
            .ToList();

        return new GradeChanges<T>(added, removed, IsFirstRun: false);
    }
}
