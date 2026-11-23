# SiufpGrades

[![CI](https://github.com/filipeoliv02/SiufpGrades/actions/workflows/ci.yml/badge.svg)](https://github.com/filipeoliv02/SiufpGrades/actions/workflows/ci.yml)

A small .NET console app that logs into the Universidade Fernando Pessoa student portal
(`portal.ufp.pt`) with a headless Chrome browser, reads your **partial** and **final** grades,
and tells you when something new appears.

I wrote it because the portal had no working notifications, so checking for new grades meant
logging in by hand again and again. Run it on a schedule (cron, systemd timer, Task Scheduler)
and it does the checking for you.

> **Disclaimer.** This is an unofficial personal project. It is not affiliated with or endorsed
> by Universidade Fernando Pessoa. It scrapes the portal's HTML, so it can stop working whenever
> the site changes. Use it only with your own account, keep the polling interval reasonable
> (a few times a day at most), and follow the portal's terms of use.

## Algorithms & techniques demonstrated

| Technique | What it is | How it is applied here | Where |
|-----------|------------|------------------------|-------|
| Multiset (bag) difference | Compare two collections ignoring order but counting duplicates. | `GradeDiff.Compare` builds a value → count dictionary of the previous grades, consumes one count per current grade (leftovers in `current` are *added*, unconsumed counts are *removed*). A changed grade appears as one removal plus one addition. O(n + m) expected time with hashing, versus O(n·m) for naive list comparison; a plain set diff would miss duplicated rows. | [GradeDiff.cs](SiufpGrades.Core/GradeDiff.cs) |
| Value-equality records | C# `record` types compare by value, so they work directly as dictionary keys. | `PartialGrade` / `FinalGrade` are immutable records; the diff relies on their generated `Equals`/`GetHashCode`. Options use `with` expressions for non-destructive updates. | [Grades.cs](SiufpGrades.Core/Grades.cs), [CommandLineOptions.cs](SiufpGrades.Core/CommandLineOptions.cs) |
| Character-level state-machine parser (RFC 4180 CSV) | Single pass over the input with an "inside quotes" state. | Handles quoted fields, escaped `""`, embedded commas/newlines, CRLF and LF, and rejects unterminated quotes. The writer quotes only when needed. O(n) in input length, no regex. | [Csv.cs](SiufpGrades.Core/Csv.cs) |
| Atomic file replace | Write to a temp file, then rename over the target. | `GradeStore.Save` writes `*.tmp` and `File.Move(..., overwrite: true)`, so a crash never leaves a truncated CSV that would cause false "new grade" alerts on the next run. | [GradeStore.cs](SiufpGrades.Core/GradeStore.cs) |
| Path-traversal guard | Accept only bare file names. | `PathFor` rejects any name where `Path.GetFileName(name) != name`, so storage cannot escape the data directory. | [GradeStore.cs](SiufpGrades.Core/GradeStore.cs) |
| Selenium explicit waits | Poll a condition until true or timeout, instead of fixed sleeps. | A single `WebDriverWait` (ignoring `NoSuchElement`/`StaleElementReference`) waits for the login field, for `document.readyState == "complete"`, and for navigation away from the login page, which doubles as login-failure detection. | [PortalClient.cs](SiufpGrades/PortalClient.cs) |
| Separation of I/O from logic (ports and adapters, lightweight) | Keep the testable core free of browser and console dependencies. | `SiufpGrades.Core` has no Selenium reference: the scraper returns raw cell text and `GradeTableParser` maps it to records. Credential lookup takes `getEnv` and prompt delegates, so it is unit-tested without a real environment or terminal. Notification strategies are also Selenium-, email-, and portal-free. | [GradeTableParser.cs](SiufpGrades.Core/GradeTableParser.cs), [Credentials.cs](SiufpGrades.Core/Credentials.cs), [Notifications/](SiufpGrades.Core/Notifications/) |
| Retry with exponential backoff and jitter | Bound repeated transient portal failures instead of failing or hammering the service on the first network/driver error. | The CLI retries one complete scrape up to three times, with capped exponential equal jitter and injectable delay/random functions. Login and grade-format errors remain fatal. | [RetryPolicy.cs](SiufpGrades.Core/Resilience/RetryPolicy.cs), [Program.cs](SiufpGrades/Program.cs) |
| Strategy / composite notification | Dispatch user notifications through small interchangeable implementations. | `INotifier` + `GradeNotification` is the core contract; the app provides a console adapter and a composite that supports multiple strategies. Tests use fake notifiers; no live email or portal call is involved. | [Notifications/](SiufpGrades.Core/Notifications/), [ConsoleNotifier.cs](SiufpGrades/ConsoleNotifier.cs) |
| Secret hygiene | Keep passwords out of argv, logs and `ToString()`. | Credentials come only from environment variables or a masked prompt; positional arguments are rejected without echoing them; `Credentials.ToString()` redacts the password. | [Credentials.cs](SiufpGrades.Core/Credentials.cs), [CommandLineOptions.cs](SiufpGrades.Core/CommandLineOptions.cs), [ConsolePrompt.cs](SiufpGrades/ConsolePrompt.cs) |
| Masked terminal input | Read keys with echo suppressed. | `ReadSecret` uses `Console.ReadKey(intercept: true)`, prints `*` per character and handles Backspace/Escape. | [ConsolePrompt.cs](SiufpGrades/ConsolePrompt.cs) |
| Deterministic resource cleanup | `IDisposable` + `using` with `try`/`finally`. | `PortalClient.Dispose` always calls `Quit()` so no orphan Chrome/chromedriver processes are left, even on errors. | [PortalClient.cs](SiufpGrades/PortalClient.cs) |
| Exit codes as an interface | Encode the outcome in the process status. | `0` no change, `2` changes, `1` error, so schedulers and shell scripts can react without parsing output. | [Program.cs](SiufpGrades/Program.cs) |
| Data-driven unit tests | xUnit `[Theory]` + `[InlineData]`. | Cover CSV escaping/parsing edge cases, multiset diff behaviour, argument validation and store/tracker round trips. | [SiufpGrades.Tests/](SiufpGrades.Tests/) |

## How it works

1. Reads your credentials from environment variables, or asks for them interactively.
2. Starts Chrome through Selenium, signs in, and reads the grade tables from
   `Notas/Parcial.aspx` and `Notas/FinalProv.aspx`.
3. Compares the result with the copy saved on the previous run, prints anything that was
   added or changed, and saves the new state as CSV.

```
Partial grades: changes detected! (1 new/updated, 0 removed/replaced)
  + Some Course - Test 2: 15
Final grades: nothing new.
```

| Exit code | Meaning                                             |
|-----------|-----------------------------------------------------|
| `0`       | No changes (or first run, which only saves a baseline) |
| `2`       | New or changed grades found                         |
| `1`       | Error (bad arguments, login failure, browser error) |

The built-in strategy writes grade-change reports to the console. Notification dispatch is implemented
through `INotifier` and `CompositeNotifier` in `SiufpGrades.Core`, so applications and tests can add or
fake other strategies without live portal, email, or OS-service calls. The exit code can also trigger
an external notification, for example `SiufpGrades; [ $? -eq 2 ] && notify-send "New grades!"`.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (LTS)
- Google Chrome or Chromium. You don't need to install chromedriver: **Selenium Manager**
  (bundled with Selenium.WebDriver) finds or downloads a matching driver on the first run.

## Configuration

### Credentials

The app never accepts credentials as command-line arguments, because those end up in shell
history and in the process list.

| Variable         | Purpose               |
|------------------|-----------------------|
| `SIUFP_USER`     | Your portal user id   |
| `SIUFP_PASSWORD` | Your portal password  |

If either variable is missing and the app is running in a terminal, it prompts for the value
and masks the password as you type. In non-interactive runs (cron, CI) both variables are
required.

To keep the password out of your shell history, put it in a file that only you can read
instead of typing `export SIUFP_PASSWORD=...`:

```sh
# ~/.config/siufpgrades/env   (chmod 600)
SIUFP_USER=12345
SIUFP_PASSWORD=...
```

```sh
set -a; . ~/.config/siufpgrades/env; set +a
dotnet run --project SiufpGrades
```

A systemd user service can load the same file with `EnvironmentFile=`.

### Options

```
--data-dir <path>   Where grade CSVs are stored
--show-browser      Show the Chrome window (default: headless)
--timeout <secs>    Max seconds to wait for each page (default: 30)
-h, --help          Show help
```

Grade data goes to `--data-dir` if given, otherwise `$SIUFP_DATA_DIR`, otherwise the per-user
app-data folder (`~/.local/share/SiufpGrades` on Linux, `%LOCALAPPDATA%\SiufpGrades` on Windows).
These files contain your personal grades. Keep them out of version control; the repo's
`.gitignore` already excludes `*.csv`.

## Build, run, test

```sh
dotnet build
dotnet test
dotnet run --project SiufpGrades -- --help
```

If .NET isn't installed locally, you can build and test with Docker:

```sh
docker run --rm -u "$(id -u):$(id -g)" -e HOME=/tmp -v "$PWD":/src -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 dotnet test
```

Example cron entry (every 3 hours):

```cron
0 */3 * * * set -a; . $HOME/.config/siufpgrades/env; set +a; /path/to/SiufpGrades >> $HOME/.local/state/siufpgrades.log 2>&1
```

## Project layout

| Path                 | Contents |
|----------------------|----------|
| `SiufpGrades/`       | Console entry point, Selenium portal client, masked console prompt |
| `SiufpGrades.Core/`  | Selenium-free logic: grade models, table parsing, RFC 4180 CSV, diffing, storage, CLI/credential handling |
| `SiufpGrades.Tests/` | xUnit v3 unit tests for the core logic |

CI (GitHub Actions) builds, tests, and checks for vulnerable NuGet packages on Linux and
Windows. Dependabot keeps NuGet packages and Actions up to date.

## License

No license file is currently included; all rights reserved by the author.
