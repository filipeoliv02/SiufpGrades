using SiufpGrades.Core;

namespace SiufpGrades.Tests;

public class CsvTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    [InlineData(" padded ", "\" padded \"")]
    public void EscapeField_QuotesOnlyWhenNeeded(string input, string expected) =>
        Assert.Equal(expected, Csv.EscapeField(input));

    [Fact]
    public void EscapeField_Null_IsEmpty() => Assert.Equal(string.Empty, Csv.EscapeField(null));

    [Fact]
    public void WriteThenParse_RoundTripsTrickyValues()
    {
        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "Course", "Type", "Grade" },
            new[] { "Algebra, Linear", "Exam \"A\"", "15,5" },
            new[] { "Multi\r\nLine", "", "  spaced  " },
            new[] { "Programação", "Época Normal", "18" },
        };

        var parsed = Csv.Parse(Csv.Write(rows));

        Assert.Equal(rows.Count, parsed.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            Assert.Equal(rows[i], parsed[i]);
        }
    }

    [Fact]
    public void Parse_HandlesLfAndMissingTrailingNewline()
    {
        var parsed = Csv.Parse("a,b\nc,d");
        Assert.Equal([["a", "b"], ["c", "d"]], parsed);
    }

    [Fact]
    public void Parse_KeepsEmptyTrailingField()
    {
        var parsed = Csv.Parse("a,\r\n");
        Assert.Equal([["a", ""]], parsed);
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        var parsed = Csv.Parse("a\r\n\r\nb\r\n");
        Assert.Equal([["a"], ["b"]], parsed);
    }

    [Fact]
    public void Parse_UnterminatedQuote_Throws() =>
        Assert.Throws<FormatException>(() => Csv.Parse("\"abc"));
}
