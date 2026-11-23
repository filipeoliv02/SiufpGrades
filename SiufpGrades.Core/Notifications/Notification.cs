namespace SiufpGrades.Core.Notifications;

/// <summary>One user-facing notification emitted after grade comparison.</summary>
public sealed record GradeNotification(string Title, IReadOnlyList<string> Lines);

/// <summary>Pluggable notification sink; implementations must not perform portal or email I/O.</summary>
public interface INotifier
{
    Task NotifyAsync(GradeNotification notification, CancellationToken cancellationToken = default);
}
