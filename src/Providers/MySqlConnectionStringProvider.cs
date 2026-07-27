using System.Globalization;

namespace ConnStringDoctor.Providers;

/// <summary>
/// Provider strategy for MySQL / MariaDB connection strings.
/// </summary>
public sealed class MySqlConnectionStringProvider : ConnectionStringProviderBase
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["server"] = "Server",
        ["host"] = "Server",
        ["database"] = "Database",
        ["db"] = "Database",
        ["user"] = "User ID",
        ["username"] = "User ID",
        ["uid"] = "User ID",
        ["password"] = "Password",
        ["pwd"] = "Password",
        ["port"] = "Port",
        ["sslmode"] = "SSL Mode",
    };

    /// <inheritdoc />
    public override DbProvider Kind => DbProvider.MySql;

    /// <inheritdoc />
    public override int DefaultPort => 3306;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> KeyAliases => Aliases;

    /// <summary>
    /// Determines whether the connection string carries MySQL specific signals: the default
    /// MySQL port (3306), or a "user"/"username" keyword alongside a server/host.
    /// </summary>
    /// <param name="raw">The raw connection string.</param>
    /// <returns>true when a MySQL specific signal is present; otherwise, false.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    public override bool CanHandle(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);

        bool hasServer = false;
        bool hasUserKeyword = false;

        foreach (var pair in ConnectionStringTokenizer.Split(raw))
        {
            var (key, value) = ConnectionStringTokenizer.SplitPair(pair);
            if (key is null || value is null)
            {
                continue;
            }

            string normalized = key.Trim().ToLowerInvariant();

            if (normalized is "server" or "host")
            {
                hasServer = true;
            }

            if (normalized is "user" or "username")
            {
                hasUserKeyword = true;
            }

            if (normalized == "port" &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) &&
                port == 3306)
            {
                return true;
            }
        }

        return hasServer && hasUserKeyword;
    }
}
