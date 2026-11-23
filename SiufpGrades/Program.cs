using OpenQA.Selenium;
using SiufpGrades;
using SiufpGrades.Core;
using SiufpGrades.Core.Notifications;
using SiufpGrades.Core.Resilience;

const int ExitNoChanges = 0;
const int ExitError = 1;
const int ExitChanges = 2;

CommandLineOptions options;
try
{
    options = CommandLineOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(CommandLineOptions.Usage);
    return ExitError;
}

if (options.ShowHelp)
{
    Console.WriteLine(CommandLineOptions.Usage);
    return ExitNoChanges;
}

Credentials credentials;
try
{
    var interactive = ConsolePrompt.IsInteractive;
    credentials = Credentials.Resolve(
        Environment.GetEnvironmentVariable,
        interactive ? () => ConsolePrompt.ReadLine("UFP user id: ") : null,
        interactive ? () => ConsolePrompt.ReadSecret("Password: ") : null);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return ExitError;
}

var store = new GradeStore(options.DataDirectory ?? GradeStore.DefaultDataDirectory());
var tracker = new GradeTracker(store);

try
{
    IReadOnlyList<PartialGrade> partial;
    IReadOnlyList<FinalGrade> final;

    var retry = new RetryPolicy(
        new RetryOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromSeconds(2),
            MaxDelay = TimeSpan.FromSeconds(30),
            Jitter = JitterMode.Equal,
        },
        ex => ex is IOException
            or WebDriverException
            or TimeoutException,
        onRetry: attempt => Console.Error.WriteLine(
            $"Retrying portal access ({attempt.Attempt}/{attempt.MaxAttempts}) after {attempt.Delay.TotalMilliseconds:0} ms: {attempt.Exception.GetType().Name}"));

    (partial, final) = await retry.ExecuteAsync(async ct =>
    {
        ct.ThrowIfCancellationRequested();
        using var portal = new PortalClient(options.Headless, options.Timeout);
        portal.Login(credentials);
        var partialGrades = portal.GetPartialGrades();
        var finalGrades = portal.GetFinalGrades();
        return (partialGrades, finalGrades);
    });

    var partialChanges = tracker.UpdatePartial(partial);
    var finalChanges = tracker.UpdateFinal(final);

    var reportLines = ChangeReport.Format("Partial grades", partialChanges)
        .Concat(ChangeReport.Format("Final grades", finalChanges))
        .ToList();

    foreach (var line in reportLines)
    {
        Console.WriteLine(line);
    }
    Console.WriteLine($"Data directory: {store.DataDirectory}");

    var changed = (!partialChanges.IsFirstRun && partialChanges.HasChanges)
                  || (!finalChanges.IsFirstRun && finalChanges.HasChanges);
    if (changed)
    {
        CompositeNotifier notifier = new(new ConsoleNotifier());
        await notifier.NotifyAsync(new GradeNotification("SiufpGrades: grade changes detected", reportLines));
    }
    return changed ? ExitChanges : ExitNoChanges;
}
catch (Exception ex) when (ex is PortalException or WebDriverException or IOException or UnauthorizedAccessException or FormatException)
{
    // Messages from our code and Selenium never contain the password.
    Console.Error.WriteLine($"Error: {ex.Message}");
    return ExitError;
}
