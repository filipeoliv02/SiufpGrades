using System.Text;

namespace SiufpGrades.Core;

/// <summary>Persists grade lists as CSV files inside a data directory.</summary>
public sealed class GradeStore
{
    public const string PartialFileName = "partialGrades.csv";
    public const string FinalFileName = "finalGrades.csv";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public GradeStore(string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        DataDirectory = Path.GetFullPath(dataDirectory);
    }

    public string DataDirectory { get; }

    /// <summary>
    /// Default location: <c>$SIUFP_DATA_DIR</c> if set, otherwise the per-user local application
    /// data folder (e.g. <c>~/.local/share/SiufpGrades</c> or <c>%LOCALAPPDATA%\SiufpGrades</c>).
    /// </summary>
    public static string DefaultDataDirectory(Func<string, string?>? getEnv = null)
    {
        getEnv ??= Environment.GetEnvironmentVariable;
        var fromEnv = getEnv("SIUFP_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var baseDir = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = AppContext.BaseDirectory;
        }
        return Path.Combine(baseDir, "SiufpGrades");
    }

    /// <returns>The stored grades, or <c>null</c> if the file does not exist yet.</returns>
    public IReadOnlyList<T>? Load<T>(string fileName, Func<IReadOnlyList<string>, T> fromFields)
    {
        ArgumentNullException.ThrowIfNull(fromFields);
        var path = PathFor(fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        // First row is the header.
        return Csv.Parse(File.ReadAllText(path, Encoding.UTF8))
            .Skip(1)
            .Select(fields => fromFields(fields))
            .ToList();
    }

    public void Save<T>(string fileName, IReadOnlyList<string> header, IEnumerable<T> grades)
        where T : IGradeRecord
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(grades);
        Directory.CreateDirectory(DataDirectory);

        var path = PathFor(fileName);
        var tempPath = path + ".tmp";
        var content = Csv.Write(new[] { header }.Concat(grades.Select(g => g.ToFields())));

        // Write to a temp file and swap it in so a crash never leaves a truncated file behind.
        File.WriteAllText(tempPath, content, Utf8NoBom);
        File.Move(tempPath, path, overwrite: true);
    }

    private string PathFor(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException("Expected a bare file name.", nameof(fileName));
        }
        return Path.Combine(DataDirectory, fileName);
    }
}
