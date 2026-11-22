using SiufpGrades.Core;

namespace SiufpGrades.Tests;

public class ParserTests
{
    [Fact]
    public void ParsePartial_MapsColumnsAndTrims()
    {
        IReadOnlyList<string>[] rows =
        [
            ["101", " Course A ", "Test 1", "12", "", ""],
            ["102", "Course B", "Project", " 16 ", "", ""],
        ];

        var grades = GradeTableParser.ParsePartial(rows);

        Assert.Equal(
            [new PartialGrade("Course A", "Test 1", "12"), new PartialGrade("Course B", "Project", "16")],
            grades);
    }

    [Fact]
    public void ParsePartial_SkipsShortOrEmptyRows()
    {
        IReadOnlyList<string>[] rows =
        [
            ["only", "two"],
            ["", "", "", ""],
            ["1", "Course", "Type", "10"],
        ];

        Assert.Equal([new PartialGrade("Course", "Type", "10")], GradeTableParser.ParsePartial(rows));
    }

    [Fact]
    public void ParseFinal_FullRow_MapsAllColumns()
    {
        IReadOnlyList<string>[] rows =
        [
            ["1", "Course", "Normal", "14", "13", "14", "x", "y", "2024-07-01", "z"],
        ];

        var grade = Assert.Single(GradeTableParser.ParseFinal(rows));

        Assert.Equal(new FinalGrade("Course", "Normal", "14", "13", "14", "2024-07-01"), grade);
    }

    [Fact]
    public void ParseFinal_ShortRow_KeepsOnlyLeadingColumns()
    {
        IReadOnlyList<string>[] rows = [["1", "Course", "Normal", "Pending", "extra"]];

        var grade = Assert.Single(GradeTableParser.ParseFinal(rows));

        Assert.Equal(new FinalGrade("Course", "Normal", "Pending", "", "", ""), grade);
    }

    [Fact]
    public void ParseFinal_NoRows_ReturnsEmpty() =>
        Assert.Empty(GradeTableParser.ParseFinal([]));
}
