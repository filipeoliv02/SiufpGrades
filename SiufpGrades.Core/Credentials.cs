namespace SiufpGrades.Core;

/// <summary>Portal login. <see cref="ToString"/> never exposes the password.</summary>
public sealed class Credentials
{
    public const string UserVariable = "SIUFP_USER";
    public const string PasswordVariable = "SIUFP_PASSWORD";

    public Credentials(string user, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(user);
        ArgumentException.ThrowIfNullOrEmpty(password);
        User = user.Trim();
        Password = password;
    }

    public string User { get; }

    public string Password { get; }

    public override string ToString() => $"Credentials {{ User = {User}, Password = *** }}";

    /// <summary>
    /// Resolves credentials from environment variables, falling back to the supplied prompts
    /// for anything missing. Prompts are <c>null</c> when no interactive console is available.
    /// </summary>
    /// <exception cref="InvalidOperationException">A value is missing and cannot be prompted for.</exception>
    public static Credentials Resolve(
        Func<string, string?> getEnv,
        Func<string?>? promptUser,
        Func<string?>? promptPassword)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        var user = NullIfBlank(getEnv(UserVariable)) ?? NullIfBlank(promptUser?.Invoke())
            ?? throw new InvalidOperationException(
                $"No user id: set {UserVariable} or run interactively to be prompted.");

        var password = NullIfEmpty(getEnv(PasswordVariable)) ?? NullIfEmpty(promptPassword?.Invoke())
            ?? throw new InvalidOperationException(
                $"No password: set {PasswordVariable} or run interactively to be prompted.");

        return new Credentials(user, password);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;
}
