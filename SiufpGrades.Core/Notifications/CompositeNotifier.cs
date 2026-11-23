namespace SiufpGrades.Core.Notifications;

/// <summary>Dispatches notifications to multiple strategies, retaining registration order.</summary>
public sealed class CompositeNotifier(params INotifier[] notifiers) : INotifier
{
    private readonly IReadOnlyList<INotifier> _notifiers = notifiers ?? throw new ArgumentNullException(nameof(notifiers));

    public async Task NotifyAsync(GradeNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        foreach (var notifier in _notifiers)
        {
            await notifier.NotifyAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
