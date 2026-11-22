using SiufpGrades.Core;

namespace SiufpGrades.Tests;

public sealed class GradeStoreAndTrackerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "siufp-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsNull()
    {
        var store = new GradeStore(_dir);
        Assert.Null(store.Load(GradeStore.PartialFileName, PartialGrade.FromFields));
    }

    [Fact]
    public void SaveThenLoad_FinalGrades_RoundTrip()
    {
        var store = new GradeStore(_dir);
        FinalGrade[] grades =
        [
            new("Course, with comma", "Normal", "", "13", "14", "2024-07-01"),
            new("Course \"quoted\"", "Recurso", "15", "", "", ""),
        ];

        store.Save(GradeStore.FinalFileName, FinalGrade.Header, grades);
        var loaded = store.Load(GradeStore.FinalFileName, FinalGrade.FromFields);

        Assert.Equal(grades, loaded);
        Assert.False(File.Exists(Path.Combine(_dir, GradeStore.FinalFileName + ".tmp")));
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        var store = new GradeStore(_dir);
        store.Save(GradeStore.PartialFileName, PartialGrade.Header, [new PartialGrade("A", "T", "1")]);
        store.Save(GradeStore.PartialFileName, PartialGrade.Header, [new PartialGrade("B", "T", "2")]);

        var loaded = store.Load(GradeStore.PartialFileName, PartialGrade.FromFields);

        Assert.Equal([new PartialGrade("B", "T", "2")], loaded);
    }

    [Fact]
    public void Load_ToleratesShortRows()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, GradeStore.PartialFileName), "ClassName,GradeType,Grade\r\nA,T\r\n");

        var loaded = new GradeStore(_dir).Load(GradeStore.PartialFileName, PartialGrade.FromFields);

        Assert.Equal([new PartialGrade("A", "T", "")], loaded);
    }

    [Theory]
    [InlineData("../escape.csv")]
    [InlineData("sub/file.csv")]
    public void Load_RejectsPathsOutsideDataDirectory(string fileName) =>
        Assert.Throws<ArgumentException>(() => new GradeStore(_dir).Load(fileName, PartialGrade.FromFields));

    [Fact]
    public void Tracker_FirstRunThenNoChangeThenNewGrade()
    {
        var tracker = new GradeTracker(new GradeStore(_dir));
        var a = new PartialGrade("A", "Test", "10");
        var b = new PartialGrade("B", "Test", "11");

        var first = tracker.UpdatePartial([a]);
        Assert.True(first.IsFirstRun);

        var second = tracker.UpdatePartial([a]);
        Assert.False(second.IsFirstRun);
        Assert.False(second.HasChanges);

        var third = tracker.UpdatePartial([a, b]);
        Assert.Equal([b], third.Added);

        var fourth = tracker.UpdatePartial([a, b]);
        Assert.False(fourth.HasChanges);
    }

    [Fact]
    public void Tracker_FirstRunWithNoGrades_StillCreatesBaseline()
    {
        var store = new GradeStore(_dir);
        var tracker = new GradeTracker(store);

        tracker.UpdateFinal([]);

        Assert.NotNull(store.Load(GradeStore.FinalFileName, FinalGrade.FromFields));
    }

    [Fact]
    public void DefaultDataDirectory_PrefersEnvironmentVariable()
    {
        var dir = GradeStore.DefaultDataDirectory(name => name == "SIUFP_DATA_DIR" ? "/custom/dir" : null);
        Assert.Equal("/custom/dir", dir);
    }

    [Fact]
    public void DefaultDataDirectory_FallsBackToAppDataFolder()
    {
        var dir = GradeStore.DefaultDataDirectory(_ => null);
        Assert.Equal("SiufpGrades", Path.GetFileName(dir));
    }
}
