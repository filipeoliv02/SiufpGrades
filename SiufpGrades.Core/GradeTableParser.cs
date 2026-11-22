namespace SiufpGrades.Core;

/// <summary>
/// Maps the raw cell text of the portal's result tables to grade records.
/// Kept free of Selenium so it can be unit tested.
/// </summary>
public static class GradeTableParser
{
    // Column positions in the portal's tables (column 0 is the course code).
    private const int CourseColumn = 1;
    private const int TypeColumn = 2;
    private const int PartialGradeColumn = 3;

    private const int OralColumn = 3;
    private const int WrittenColumn = 4;
    private const int FinalColumn = 5;
    private const int RegisteredOnColumn = 8;
    private const int FullFinalRowCellCount = 10;

    /// <param name="rows">Data rows (header excluded), each a list of cell texts.</param>
    public static IReadOnlyList<PartialGrade> ParsePartial(IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows
            .Where(cells => cells.Count > PartialGradeColumn)
            .Select(cells => new PartialGrade(
                Clean(cells[CourseColumn]),
                Clean(cells[TypeColumn]),
                Clean(cells[PartialGradeColumn])))
            .Where(g => g.Course.Length > 0)
            .ToList();
    }

    /// <param name="rows">Data rows (header excluded), each a list of cell texts.</param>
    public static IReadOnlyList<FinalGrade> ParseFinal(IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows
            .Where(cells => cells.Count > OralColumn)
            .Select(cells =>
            {
                var full = cells.Count >= FullFinalRowCellCount;
                return new FinalGrade(
                    Clean(cells[CourseColumn]),
                    Clean(cells[TypeColumn]),
                    Clean(cells[OralColumn]),
                    full ? Clean(cells[WrittenColumn]) : string.Empty,
                    full ? Clean(cells[FinalColumn]) : string.Empty,
                    full ? Clean(cells[RegisteredOnColumn]) : string.Empty);
            })
            .Where(g => g.Course.Length > 0)
            .ToList();
    }

    private static string Clean(string? text) => text?.Trim() ?? string.Empty;
}
