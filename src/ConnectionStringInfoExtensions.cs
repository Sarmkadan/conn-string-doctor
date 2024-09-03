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
    public static string GetServerEndpoint(this ConnectionStringInfo info)
    {
        if (string.IsNullOrEmpty(info.Server))
        {
            return string.Empty;
        }

        if (info.Port.HasValue && info.Port.Value > 0)
        {
            return $"{info.Server}:{info.Port.Value}";
        }

        return info.Server;
    }

    /// <summary>
    /// Gets a value indicating whether this connection represents a local database (SQLite or local SQL Server).
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>True if the database is local; otherwise, false.</returns>
    public static bool IsLocalDatabase(this ConnectionStringInfo info)
    {
        return info.Provider == DbProvider.Sqlite ||
               (info.Provider == DbProvider.SqlServer &&
                (string.Equals(info.Server, "localhost", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(info.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                 string.IsNullOrEmpty(info.Server)));
    }

    /// <summary>
    /// Gets the connection string with sensitive information (password) removed.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>A sanitized connection string without password.</returns>
    public static string ToSanitizedString(this ConnectionStringInfo info)
    {
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
    public static bool RequiresAuthentication(this ConnectionStringInfo info)
    {
        return !string.IsNullOrEmpty(info.User) && !string.IsNullOrEmpty(info.Password);
    }
}