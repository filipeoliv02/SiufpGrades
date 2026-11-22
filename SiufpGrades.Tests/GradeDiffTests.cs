using SiufpGrades.Core;

namespace SiufpGrades.Tests;

public class GradeDiffTests
{
    private static readonly PartialGrade A = new("Course A", "Test 1", "12");
    private static readonly PartialGrade B = new("Course B", "Project", "16");
    private static readonly PartialGrade C = new("Course C", "Test 1", "9");

    [Fact]
    public void NoPreviousData_IsFirstRun_AllAdded()
    {
        var changes = GradeDiff.Compare(null, [A, B]);

        Assert.True(changes.IsFirstRun);
        Assert.Equal([A, B], changes.Added);
        Assert.Empty(changes.Removed);
    }

    [Fact]
    public void SameGradesDifferentOrder_NoChanges()
    {
        var changes = GradeDiff.Compare([A, B, C], [C, A, B]);

        Assert.False(changes.IsFirstRun);
        Assert.False(changes.HasChanges);
    }

    [Fact]
    public void NewGrade_IsReportedAsAdded()
    {
        var changes = GradeDiff.Compare([A], [A, B]);

        Assert.True(changes.HasChanges);
        Assert.Equal([B], changes.Added);
        Assert.Empty(changes.Removed);
    }

    [Fact]
    public void ChangedValue_IsReportedAsRemovedPlusAdded()
    {
        var updated = A with { Grade = "14" };

        var changes = GradeDiff.Compare([A, B], [updated, B]);

        Assert.Equal([updated], changes.Added);
        Assert.Equal([A], changes.Removed);
    }

    [Fact]
    public void DisappearedGrade_IsReportedAsRemoved()
    {
        var changes = GradeDiff.Compare([A, B], [B]);

        Assert.Empty(changes.Added);
        Assert.Equal([A], changes.Removed);
    }

    [Fact]
    public void Duplicates_AreCounted()
    {
        var changes = GradeDiff.Compare([A], [A, A]);

        Assert.Equal([A], changes.Added);
        Assert.Empty(changes.Removed);
    }

    [Fact]
    public void EmptyToEmpty_NoChanges() =>
        Assert.False(GradeDiff.Compare(Array.Empty<PartialGrade>(), []).HasChanges);
}
