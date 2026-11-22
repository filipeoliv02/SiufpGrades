using System.Text;

namespace SiufpGrades.Core;

/// <summary>Minimal RFC 4180 CSV writer/reader (comma separator, double-quote escaping).</summary>
public static class Csv
{
    private static readonly char[] CharsRequiringQuotes = [',', '"', '\r', '\n'];

    public static string EscapeField(string? value)
    {
        value ??= string.Empty;
        var needsQuotes = value.IndexOfAny(CharsRequiringQuotes) >= 0
            || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1])));
        return needsQuotes ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
    }

    public static string Write(IEnumerable<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.AppendJoin(',', row.Select(EscapeField));
            sb.Append("\r\n");
        }
        return sb.ToString();
    }

    public static List<List<string>> Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var fieldStarted = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    fieldStarted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = true;
                    break;
                case '\r':
                case '\n':
                    if (c == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }
                    if (fieldStarted || field.Length > 0 || row.Count > 0)
                    {
                        row.Add(field.ToString());
                        rows.Add(row);
                    }
                    row = [];
                    field.Clear();
                    fieldStarted = false;
                    break;
                default:
                    field.Append(c);
                    fieldStarted = true;
                    break;
            }
        }

        if (inQuotes)
        {
            throw new FormatException("Unterminated quoted field in CSV content.");
        }

        if (fieldStarted || field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
