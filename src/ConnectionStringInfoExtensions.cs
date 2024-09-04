namespace ConnStringDoctor;

/// <summary>
/// Provides extension methods for <see cref="ConnectionStringInfo"/> to simplify common connection string operations.
/// </summary>
public static class ConnectionStringInfoExtensions
{
    /// <summary>
    /// Gets the normalized server endpoint in the format "server:port" or just "server" if port is not specified.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>The normalized server endpoint string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
    public static string GetServerEndpoint(this ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return info.Port.HasValue && info.Port.Value > 0
            ? $"{info.Server}:{info.Port.Value}"
            : info.Server ?? string.Empty;
    }

    /// <summary>
    /// Gets a value indicating whether this connection represents a local database (SQLite or local SQL Server).
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>True if the database is local; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
    public static bool IsLocalDatabase(this ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return info.Provider == DbProvider.Sqlite ||
            (info.Provider == DbProvider.SqlServer &&
            (info.Server is null ||
            string.Equals(info.Server, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(info.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(info.Server, "::1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(info.Server, ".", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Gets the connection string with sensitive information (password) removed.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>A sanitized connection string without password.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
    public static string ToSanitizedString(this ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var sb = new System.Text.StringBuilder();
        sb.Append($"Provider={info.Provider}");

        if (!string.IsNullOrEmpty(info.Server))
        {
            sb.Append($";Server={info.Server}");
            if (info.Port.HasValue)
            {
                sb.Append($":{info.Port}");
            }
        }

        if (!string.IsNullOrEmpty(info.Database))
        {
            sb.Append($";Database={info.Database}");
        }

        if (!string.IsNullOrEmpty(info.User))
        {
            sb.Append($";User={info.User}");
        }

        if (info.Properties.Count > 0)
        {
            foreach (var prop in info.Properties)
            {
                sb.Append($";{prop.Key}={prop.Value}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a value indicating whether this connection requires authentication (has user and password).
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>True if authentication is required; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="info"/> is <see langword="null"/>.</exception>
    public static bool RequiresAuthentication(this ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return !string.IsNullOrEmpty(info.User) && !string.IsNullOrEmpty(info.Password);
    }
}
