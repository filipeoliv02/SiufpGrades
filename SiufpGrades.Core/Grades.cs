namespace SiufpGrades.Core;

/// <summary>A grade row that can be persisted as a CSV record.</summary>
public interface IGradeRecord
{
    /// <summary>Field values in CSV column order.</summary>
    IReadOnlyList<string> ToFields();
}

/// <summary>A partial (continuous assessment) grade as shown on the portal's "Parcial" page.</summary>
public sealed record PartialGrade(string Course, string GradeType, string Grade) : IGradeRecord
{
    public static readonly IReadOnlyList<string> Header = ["Course", "GradeType", "Grade"];

    public IReadOnlyList<string> ToFields() => [Course, GradeType, Grade];

    public static PartialGrade FromFields(IReadOnlyList<string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new PartialGrade(At(fields, 0), At(fields, 1), At(fields, 2));
    }

    internal static string At(IReadOnlyList<string> fields, int index) =>
        index < fields.Count ? fields[index] : string.Empty;

    public override string ToString() => $"{Course} - {GradeType}: {Grade}";
}

/// <summary>A final/provisional grade as shown on the portal's "FinalProv" page.</summary>
public sealed record FinalGrade(
    string Course,
    string GradeType,
    string Oral,
    string Written,
    string Final,
    string RegisteredOn) : IGradeRecord
{
    public static readonly IReadOnlyList<string> Header =
        ["Course", "GradeType", "Oral", "Written", "Final", "RegisteredOn"];

    public IReadOnlyList<string> ToFields() => [Course, GradeType, Oral, Written, Final, RegisteredOn];

    public static FinalGrade FromFields(IReadOnlyList<string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        return new FinalGrade(
            PartialGrade.At(fields, 0),
            PartialGrade.At(fields, 1),
            PartialGrade.At(fields, 2),
            PartialGrade.At(fields, 3),
            PartialGrade.At(fields, 4),
            PartialGrade.At(fields, 5));
    }

    public override string ToString() =>
        $"{Course} - {GradeType}: final {Final} (oral {Oral}, written {Written}, registered {RegisteredOn})";
}
