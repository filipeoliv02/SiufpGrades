namespace SiufpGrades.Core.Resilience;

/// <summary>How random jitter is applied to the exponential backoff ceiling.</summary>
public enum JitterMode
{
    /// <summary>Deterministic: wait exactly the (capped) exponential delay.</summary>
    None,

    /// <summary>"Full jitter": uniform in [0, ceiling). Spreads retries best; can be very short.</summary>
    Full,

    /// <summary>"Equal jitter": ceiling/2 + uniform in [0, ceiling/2). Keeps a guaranteed minimum wait.</summary>
    Equal,
}

public sealed record RetryOptions
{
    /// <summary>Total attempts including the first one (1 = no retries).</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Delay ceiling before the first retry; doubles for every further retry.</summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Upper bound for any single delay.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    public JitterMode Jitter { get; init; } = JitterMode.Full;
}

/// <summary>Information about a failed attempt that is about to be retried.</summary>
/// <param name="Attempt">1-based number of the attempt that just failed.</param>
/// <param name="MaxAttempts">Configured total number of attempts.</param>
/// <param name="Delay">How long the policy waits before the next attempt.</param>
/// <param name="Exception">The transient failure.</param>
public sealed record RetryAttempt(int Attempt, int MaxAttempts, TimeSpan Delay, Exception Exception);

/// <summary>
/// Retries an operation on transient failures using capped exponential backoff with jitter
/// (see "Exponential Backoff And Jitter", AWS Architecture Blog). The delay function and the
/// random source are injectable, so the algorithm is tested without real waiting.
/// </summary>
public sealed class RetryPolicy
{
    private readonly RetryOptions _options;
    private readonly Func<Exception, bool> _isTransient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Func<double> _random;
    private readonly Action<RetryAttempt>? _onRetry;

    /// <param name="options">Attempts and delay bounds.</param>
    /// <param name="isTransient">Decides which failures are worth retrying. Anything else is rethrown immediately.</param>
    /// <param name="delay">Waits for the given time; defaults to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
    /// <param name="random">Returns a value in [0, 1); defaults to <see cref="Random.Shared"/>.</param>
    /// <param name="onRetry">Called before each wait, e.g. for logging.</param>
    public RetryPolicy(
        RetryOptions options,
        Func<Exception, bool> isTransient,
        Func<TimeSpan, CancellationToken, Task>? delay = null,
        Func<double>? random = null,
        Action<RetryAttempt>? onRetry = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(isTransient);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxAttempts, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BaseDelay, TimeSpan.Zero, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDelay, options.BaseDelay, nameof(options));

        _options = options;
        _isTransient = isTransient;
        _delay = delay ?? Task.Delay;
        // Jitter only needs statistical spread, not unpredictability, so a non-crypto RNG is fine.
#pragma warning disable CA5394 // Do not use insecure randomness
        _random = random ?? Random.Shared.NextDouble;
#pragma warning restore CA5394
        _onRetry = onRetry;
    }

    /// <summary>A policy that never retries.</summary>
    public static RetryPolicy None { get; } = new(new RetryOptions { MaxAttempts = 1 }, _ => false);

    public RetryOptions Options => _options;

    /// <summary>
    /// Exponential ceiling for the n-th retry: <c>min(MaxDelay, BaseDelay * 2^(n-1))</c>.
    /// Computed in floating point so large <paramref name="retryNumber"/> values saturate instead of overflowing.
    /// </summary>
    public static TimeSpan BackoffCeiling(RetryOptions options, int retryNumber)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryNumber, 1);
        var ticks = options.BaseDelay.Ticks * Math.Pow(2, retryNumber - 1);
        return TimeSpan.FromTicks((long)Math.Min(options.MaxDelay.Ticks, ticks));
    }

    /// <summary>Delay before the n-th retry (1-based), with jitter applied.</summary>
    public TimeSpan ComputeDelay(int retryNumber)
    {
        var ceiling = BackoffCeiling(_options, retryNumber).Ticks;
        var r = Math.Clamp(_random(), 0d, 1d);
        var ticks = _options.Jitter switch
        {
            JitterMode.None => ceiling,
            JitterMode.Full => (long)(ceiling * r),
            JitterMode.Equal => (ceiling / 2) + (long)(ceiling / 2 * r),
            _ => throw new InvalidOperationException($"Unknown jitter mode {_options.Jitter}."),
        };
        return TimeSpan.FromTicks(ticks);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _options.MaxAttempts
                                       && !cancellationToken.IsCancellationRequested
                                       && _isTransient(ex))
            {
                var delay = ComputeDelay(attempt);
                _onRetry?.Invoke(new RetryAttempt(attempt, _options.MaxAttempts, delay, ex));
                await _delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteAsync<bool>(
            async ct =>
            {
                await operation(ct).ConfigureAwait(false);
                return true;
            },
            cancellationToken);
    }

    /// <summary>Retries a synchronous operation (e.g. a blocking Selenium call).</summary>
    public Task<T> ExecuteAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return ExecuteAsync(_ => Task.FromResult(operation()), cancellationToken);
    }
}
