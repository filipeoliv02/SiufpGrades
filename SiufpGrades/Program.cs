using OpenQA.Selenium;
using SiufpGrades;
using SiufpGrades.Core;

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

    using (var portal = new PortalClient(options.Headless, options.Timeout))
    {
        portal.Login(credentials);
        partial = portal.GetPartialGrades();
        final = portal.GetFinalGrades();
    }

    var partialChanges = tracker.UpdatePartial(partial);
    var finalChanges = tracker.UpdateFinal(final);

    foreach (var line in ChangeReport.Format("Partial grades", partialChanges)
                 .Concat(ChangeReport.Format("Final grades", finalChanges)))
    {
        Console.WriteLine(line);
    }
    Console.WriteLine($"Data directory: {store.DataDirectory}");

    var changed = (!partialChanges.IsFirstRun && partialChanges.HasChanges)
                  || (!finalChanges.IsFirstRun && finalChanges.HasChanges);
    return changed ? ExitChanges : ExitNoChanges;
}
catch (Exception ex) when (ex is PortalException or WebDriverException or IOException or UnauthorizedAccessException or FormatException)
{
    // Messages from our code and Selenium never contain the password.
    Console.Error.WriteLine($"Error: {ex.Message}");
    return ExitError;
}
