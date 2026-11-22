using SiufpGrades.Core;

namespace SiufpGrades.Tests;

public class CliAndCredentialsTests
{
    private static Func<string, string?> Env(params (string Key, string Value)[] vars) =>
        name => vars.FirstOrDefault(v => v.Key == name).Value;

    [Fact]
    public void Resolve_UsesEnvironmentWithoutPrompting()
    {
        var creds = Credentials.Resolve(
            Env(("SIUFP_USER", "12345"), ("SIUFP_PASSWORD", "s3cret")),
            () => throw new InvalidOperationException("should not prompt"),
            () => throw new InvalidOperationException("should not prompt"));

        Assert.Equal("12345", creds.User);
        Assert.Equal("s3cret", creds.Password);
    }

    [Fact]
    public void Resolve_FallsBackToPrompt()
    {
        var creds = Credentials.Resolve(Env(("SIUFP_USER", "12345")), () => "ignored", () => "typed");

        Assert.Equal("12345", creds.User);
        Assert.Equal("typed", creds.Password);
    }

    [Fact]
    public void Resolve_NonInteractiveAndMissing_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Credentials.Resolve(Env(("SIUFP_USER", "u")), null, null));
        Assert.Contains("SIUFP_PASSWORD", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToString_NeverContainsPassword()
    {
        var creds = new Credentials("user", "hunter2");
        Assert.DoesNotContain("hunter2", creds.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_Defaults()
    {
        var o = CommandLineOptions.Parse([]);

        Assert.True(o.Headless);
        Assert.False(o.ShowHelp);
        Assert.Null(o.DataDirectory);
        Assert.Equal(TimeSpan.FromSeconds(30), o.Timeout);
    }

    [Fact]
    public void Parse_AllOptions()
    {
        var o = CommandLineOptions.Parse(["--show-browser", "--data-dir", "/tmp/x", "--timeout", "45"]);

        Assert.False(o.Headless);
        Assert.Equal("/tmp/x", o.DataDirectory);
        Assert.Equal(TimeSpan.FromSeconds(45), o.Timeout);
    }

    [Fact]
    public void Parse_LegacyPositionalCredentials_RejectedWithoutEchoingThem()
    {
        var ex = Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse(["12345", "hunter2"]));

        Assert.DoesNotContain("hunter2", ex.Message, StringComparison.Ordinal);
        Assert.Contains("SIUFP_PASSWORD", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--timeout")]
    [InlineData("--timeout", "0")]
    [InlineData("--timeout", "abc")]
    [InlineData("--data-dir")]
    [InlineData("--bogus")]
    public void Parse_InvalidArguments_Throw(params string[] args) =>
        Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse(args));

    [Fact]
    public void ChangeReport_ListsAddedAndRemoved()
    {
        var oldGrade = new PartialGrade("A", "T", "10");
        var newGrade = new PartialGrade("A", "T", "12");
        var lines = ChangeReport.Format("Partial", GradeDiff.Compare([oldGrade], [newGrade]));

        Assert.Equal(3, lines.Count);
        Assert.Contains("changes detected", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("  + ", lines[1], StringComparison.Ordinal);
        Assert.StartsWith("  - ", lines[2], StringComparison.Ordinal);
    }

    [Fact]
    public void ChangeReport_NothingNew() =>
        Assert.Equal(["Partial: nothing new."], ChangeReport.Format("Partial", GradeDiff.Compare<PartialGrade>([], [])));
}
