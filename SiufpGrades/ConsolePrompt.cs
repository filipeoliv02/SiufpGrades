using System.Text;

namespace SiufpGrades;

internal static class ConsolePrompt
{
    public static bool IsInteractive => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    public static string? ReadLine(string label)
    {
        Console.Write(label);
        return Console.ReadLine();
    }

    /// <summary>Reads a line without echoing the typed characters (a '*' is shown per character).</summary>
    public static string? ReadSecret(string label)
    {
        Console.Write(label);
        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return buffer.ToString();
                case ConsoleKey.Backspace:
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                    }
                    break;
                case ConsoleKey.Escape:
                    Console.WriteLine();
                    return null;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        buffer.Append(key.KeyChar);
                        Console.Write('*');
                    }
                    break;
            }
        }
    }
}
