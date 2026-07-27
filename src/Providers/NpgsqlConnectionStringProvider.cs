using System.Globalization;

namespace ConnStringDoctor.Providers;

/// <summary>
/// Provider strategy for PostgreSQL (Npgsql) connection strings.
/// </summary>
public sealed class NpgsqlConnectionStringProvider : ConnectionStringProviderBase
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["host"] = "Server",
        ["server"] = "Server",
        ["database"] = "Database",
        ["dbname"] = "Database",
        ["username"] = "User ID",
        ["user id"] = "User ID",
        ["user"] = "User ID",
        ["password"] = "Password",
        ["pwd"] = "Password",
        ["port"] = "Port",
        ["ssl mode"] = "SSL Mode",
        ["sslmode"] = "SSL Mode",
    };

    /// <inheritdoc />
    public override DbProvider Kind => DbProvider.PostgreSql;

    /// <inheritdoc />
    public override int DefaultPort => 5432;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> KeyAliases => Aliases;

    /// <summary>
    /// Determines whether the connection string carries PostgreSQL specific signals:
    /// an SSL Mode keyword, or the default PostgreSQL port (5432).
    /// </summary>
    /// <param name="raw">The raw connection string.</param>
    /// <returns>true when a PostgreSQL specific signal is present; otherwise, false.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    public override bool CanHandle(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);

        foreach (var pair in ConnectionStringTokenizer.Split(raw))
        {
            var (key, value) = ConnectionStringTokenizer.SplitPair(pair);
            if (key is null || value is null)
            {
                continue;
            }

            string normalized = key.Trim().ToLowerInvariant();
            if (normalized is "ssl mode" or "sslmode")
            {
                return true;
            }

            if (normalized == "port" &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) &&
                port == 5432)
            {
                return true;
            }
        }

        return false;
    }
}
