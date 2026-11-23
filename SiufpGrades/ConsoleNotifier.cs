using SiufpGrades.Core.Notifications;

namespace SiufpGrades;

/// <summary>Console adapter for the notification strategy.</summary>
internal sealed class ConsoleNotifier : INotifier
{
    public Task NotifyAsync(GradeNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        Console.WriteLine(notification.Title);
        foreach (var line in notification.Lines)
        {
            Console.WriteLine(line);
        }
        return Task.CompletedTask;
    }
}
