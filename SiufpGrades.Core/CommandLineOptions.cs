namespace SiufpGrades.Core;

public sealed record CommandLineOptions
{
    public const string Usage = """
        Usage: SiufpGrades [options]

        Credentials are read from the SIUFP_USER and SIUFP_PASSWORD environment variables.
        If either is missing and a terminal is attached, you are prompted (password input is masked).

        Options:
          --data-dir <path>   Where grade CSVs are stored (default: $SIUFP_DATA_DIR or the
                              per-user local app data folder, e.g. ~/.local/share/SiufpGrades)
          --show-browser      Run Chrome with a visible window (default: headless)
          --timeout <secs>    Max seconds to wait for each page (default: 30)
          -h, --help          Show this help

        Exit codes: 0 = no changes, 2 = new/changed grades detected, 1 = error.
        """;

    public bool ShowHelp { get; init; }

    public bool Headless { get; init; } = true;

    public string? DataDirectory { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <exception cref="ArgumentException">Unknown or malformed arguments.</exception>
    public static CommandLineOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var options = new CommandLineOptions();

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help" or "/?":
                    options = options with { ShowHelp = true };
                    break;
                case "--show-browser":
                    options = options with { Headless = false };
                    break;
                case "--data-dir":
                    options = options with { DataDirectory = NextValue(args, ref i, arg) };
                    break;
                case "--timeout":
                    var raw = NextValue(args, ref i, arg);
                    if (!int.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, out var secs) || secs <= 0)
                    {
                        throw new ArgumentException($"--timeout expects a positive number of seconds, got '{raw}'.");
                    }
                    options = options with { Timeout = TimeSpan.FromSeconds(secs) };
                    break;
                default:
                    // Deliberately do not echo the argument: it may be a password from the old CLI syntax.
                    throw new ArgumentException(arg.StartsWith('-')
                        ? "Unknown option."
                        : "Positional arguments are no longer supported. Passing credentials on the "
                          + "command line leaks them to shell history and the process list; use the "
                          + $"{Credentials.UserVariable}/{Credentials.PasswordVariable} environment variables "
                          + "or the interactive prompt instead.");
            }
        }

        return options;
    }

    private static string NextValue(IReadOnlyList<string> args, ref int i, string name)
    {
        if (i + 1 >= args.Count || string.IsNullOrWhiteSpace(args[i + 1]))
        {
            throw new ArgumentException($"{name} requires a value.");
        }
        return args[++i];
    }
}
