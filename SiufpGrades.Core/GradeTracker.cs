namespace SiufpGrades.Core;

/// <summary>Compares freshly scraped grades with the stored copy and persists the new state.</summary>
public sealed class GradeTracker(GradeStore store)
{
    private readonly GradeStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public GradeChanges<PartialGrade> UpdatePartial(IReadOnlyList<PartialGrade> current) =>
        Update(GradeStore.PartialFileName, PartialGrade.Header, PartialGrade.FromFields, current);

    public GradeChanges<FinalGrade> UpdateFinal(IReadOnlyList<FinalGrade> current) =>
        Update(GradeStore.FinalFileName, FinalGrade.Header, FinalGrade.FromFields, current);

    private GradeChanges<T> Update<T>(
        string fileName,
        IReadOnlyList<string> header,
        Func<IReadOnlyList<string>, T> fromFields,
        IReadOnlyList<T> current)
        where T : IGradeRecord
    {
        var previous = _store.Load(fileName, fromFields);
        var changes = GradeDiff.Compare(previous, current);
        if (changes.IsFirstRun || changes.HasChanges)
        {
            _store.Save(fileName, header, current);
        }
        return changes;
    }
}
