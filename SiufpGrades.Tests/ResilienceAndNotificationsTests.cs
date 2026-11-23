using SiufpGrades.Core.Notifications;
using SiufpGrades.Core.Resilience;

namespace SiufpGrades.Tests;

public sealed class ResilienceAndNotificationsTests
{
    [Fact]
    public async Task RetryPolicy_RetriesTransientThenSucceeds()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();
        var retries = new List<RetryAttempt>();
        var policy = new RetryPolicy(
            new RetryOptions { MaxAttempts = 3, BaseDelay = TimeSpan.FromMilliseconds(10), Jitter = JitterMode.None },
            ex => ex is IOException,
            delay: (delay, _) => { delays.Add(delay); return Task.CompletedTask; },
            random: () => 0,
            onRetry: retries.Add);

        var result = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new IOException("temporary");
            }
            return Task.FromResult(42);
        }, TestContext.Current.CancellationToken);

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
        Assert.Equal([TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20)], delays);
        Assert.Equal([1, 2], retries.Select(r => r.Attempt));
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetryNonTransient()
    {
        var attempts = 0;
        var policy = new RetryPolicy(
            new RetryOptions { MaxAttempts = 3 },
            ex => ex is IOException,
            delay: (_, _) => Task.CompletedTask);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => policy.ExecuteAsync<bool>(_ =>
        {
            attempts++;
            throw new UnauthorizedAccessException("permanent");
        }, TestContext.Current.CancellationToken));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void RetryPolicy_ValidatesBounds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(
            new RetryOptions { MaxAttempts = 0 }, _ => true));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy(
            new RetryOptions { BaseDelay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(1) }, _ => true));
    }

    [Fact]
    public async Task CompositeNotifier_FansOutInOrder()
    {
        var calls = new List<string>();
        var first = new RecordingNotifier("first", calls);
        var second = new RecordingNotifier("second", calls);
        var composite = new CompositeNotifier(first, second);
        var notification = new GradeNotification("Grades", ["+ A"]);

        await composite.NotifyAsync(notification, TestContext.Current.CancellationToken);

        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public async Task CompositeNotifier_ForwardsCancellation()
    {
        var cancellation = new CancellationToken(canceled: true);
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new CompositeNotifier(new RecordingNotifier("test", [])).NotifyAsync(
                new GradeNotification("Grades", []), cancellation));
    }

    private sealed class RecordingNotifier(string name, List<string> calls) : INotifier
    {
        public Task NotifyAsync(GradeNotification notification, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            calls.Add(name);
            return Task.CompletedTask;
        }
    }
}
