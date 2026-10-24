#nullable enable

namespace ConnStringDoctor;

/// <summary>
/// Holds the per-field rules shared by <see cref="ConnectionStringInfoValidation"/> and
/// <see cref="FluentConnectionStringBuilderValidation"/>, so a server, port, database, or credential
/// is judged the same way whether it came from a parsed connection string or from in-progress builder state.
/// Each method returns a problem description, or <see langword="null"/> when the value is acceptable.
/// </summary>
internal static class ConnectionStringFieldValidation
{
    /// <summary>
    /// The lowest TCP port number accepted.
    /// </summary>
    internal const int MinPort = 1;

    /// <summary>
    /// The highest TCP port number accepted.
    /// </summary>
    internal const int MaxPort = 65535;

    /// <summary>
    /// Checks the server or host name.
    /// </summary>
    /// <param name="server">The server or host name to check.</param>
    /// <returns>A problem description when the value is null, empty, or whitespace; otherwise <see langword="null"/>.</returns>
    internal static string? ServerProblem(string? server) =>
        string.IsNullOrWhiteSpace(server) ? "Server is null or empty" : null;

    /// <summary>
    /// Checks the TCP port number. An absent port is acceptable and means the provider default applies.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>A problem description when the port is outside <see cref="MinPort"/> to <see cref="MaxPort"/>; otherwise <see langword="null"/>.</returns>
    internal static string? PortProblem(int? port) =>
        port is { } value && (value < MinPort || value > MaxPort)
            ? $"Port is out of range ({MinPort}-{MaxPort})"
            : null;

    /// <summary>
    /// Checks the database name or path.
    /// </summary>
    /// <param name="database">The database name or path to check.</param>
    /// <param name="required">Whether a missing database is a problem in the current context.</param>
    /// <returns>A problem description when the database is required but missing; otherwise <see langword="null"/>.</returns>
    internal static string? DatabaseProblem(string? database, bool required = true) =>
        required && string.IsNullOrWhiteSpace(database) ? "Database is null or empty" : null;

    /// <summary>
    /// Checks the user name.
    /// </summary>
    /// <param name="user">The user name to check.</param>
    /// <param name="required">Whether a missing user is a problem in the current context.</param>
    /// <returns>A problem description when the user is required but missing; otherwise <see langword="null"/>.</returns>
    internal static string? UserProblem(string? user, bool required = true) =>
        required && string.IsNullOrWhiteSpace(user) ? "User is null or empty" : null;

    /// <summary>
    /// Checks the password.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <param name="required">Whether a missing password is a problem in the current context.</param>
    /// <returns>A problem description when the password is required but missing; otherwise <see langword="null"/>.</returns>
    internal static string? PasswordProblem(string? password, bool required = true) =>
        required && string.IsNullOrWhiteSpace(password) ? "Password is null or empty" : null;

    /// <summary>
    /// Checks that the minimum pool size, when set, is not negative.
    /// </summary>
    /// <param name="min">The minimum pool size to check.</param>
    /// <returns>A problem description when the minimum is negative; otherwise <see langword="null"/>.</returns>
    internal static string? MinPoolSizeProblem(int? min) =>
        min < 0 ? "Minimum pool size cannot be negative" : null;

    /// <summary>
    /// Checks that the maximum pool size, when both bounds are set, is not less than the minimum.
    /// </summary>
    /// <param name="min">The minimum pool size.</param>
    /// <param name="max">The maximum pool size.</param>
    /// <returns>A problem description when the maximum is below the minimum; otherwise <see langword="null"/>.</returns>
    internal static string? PoolSizeRangeProblem(int? min, int? max) =>
        min is { } lower && max is { } upper && upper < lower
            ? "Maximum pool size cannot be less than minimum pool size"
            : null;

    /// <summary>
    /// Checks the connection timeout in seconds, when set.
    /// </summary>
    /// <param name="seconds">The timeout in seconds.</param>
    /// <returns>A problem description when the timeout is zero or negative; otherwise <see langword="null"/>.</returns>
    internal static string? TimeoutProblem(int? seconds) =>
        seconds <= 0 ? "Timeout must be a positive integer" : null;
}
