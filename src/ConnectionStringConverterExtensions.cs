using System.Globalization;

namespace ConnStringDoctor;

/// <summary>
/// Provides useful extension methods for connection string conversion types.
/// </summary>
public static class ConnectionStringConverterExtensions
{
    /// <summary>
    /// Attempts to convert the connection string to the target provider, returning a boolean indicating success.
    /// </summary>
    /// <param name="converter">The converter instance.</param>
    /// <param name="source">The parsed source connection string information.</param>
    /// <param name="targetProvider">The target provider name (e.g. "postgres", "mysql", "sqlserver").</param>
    /// <param name="result">When successful, receives the conversion result; otherwise null.</param>
    /// <returns>True if conversion succeeded; false if source or target provider is invalid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="converter"/> or <paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetProvider"/> is null or whitespace.</exception>
    public static bool TryConvert(
        this ConnectionStringConverter converter,
        ConnectionStringConverter.ConnectionStringInfo source,
        string targetProvider,
        out ConversionResult? result)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrEmpty(targetProvider);

        try
        {
            result = converter.Convert(source, targetProvider.Trim());
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ThreadAbortException)
        {
            // Only swallow expected conversion exceptions, not system exceptions
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Gets the connection string value for a specific key from the connection string info, or null if the key doesn't exist.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <param name="key">The key to look up (case-insensitive).</param>
    /// <returns>The value if found; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
    public static string? GetValue(this ConnectionStringConverter.ConnectionStringInfo info, string key)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return info.OriginalParts.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Determines whether the conversion result contains any warnings.
    /// </summary>
    /// <param name="result">The conversion result to check.</param>
    /// <returns>True if there are warnings; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static bool HasWarnings(this ConversionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Warnings.Count > 0;
    }

    /// <summary>
    /// Determines whether the conversion result has unmapped keys.
    /// </summary>
    /// <param name="result">The conversion result to check.</param>
    /// <returns>True if there are unmapped keys; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static bool HasUnmappedKeys(this ConversionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.UnmappedKeys.Count > 0;
    }

    /// <summary>
    /// Gets the first warning message, or null if there are no warnings.
    /// </summary>
    /// <param name="result">The conversion result to query.</param>
    /// <returns>The first warning message if available; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static string? FirstWarning(this ConversionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Warnings.Count > 0 ? result.Warnings[0] : null;
    }

    /// <summary>
    /// Gets the first unmapped key, or null if there are no unmapped keys.
    /// </summary>
    /// <param name="result">The conversion result to query.</param>
    /// <returns>The first unmapped key if available; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static string? FirstUnmappedKey(this ConversionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.UnmappedKeys.Count > 0 ? result.UnmappedKeys[0] : null;
    }

    /// <summary>
    /// Parses a connection string and immediately converts it to the target provider.
    /// </summary>
    /// <param name="converter">The converter instance.</param>
    /// <param name="connectionString">The connection string to parse and convert.</param>
    /// <param name="targetProvider">The target provider name.</param>
    /// <returns>The conversion result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="converter"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is null or whitespace.</exception>
    public static ConversionResult ParseAndConvert(
        this ConnectionStringConverter converter,
        string connectionString,
        string targetProvider)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(targetProvider);

        var source = ConnectionStringConverter.Parse(connectionString);
        return converter.Convert(source, targetProvider.Trim());
    }

    /// <summary>
    /// Gets the database name from the connection string parts.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>The database name if found; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
    public static string? GetDatabaseName(this ConnectionStringConverter.ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return info.OriginalParts.TryGetValue("Database", out var db) && !string.IsNullOrWhiteSpace(db)
            ? db
            : info.OriginalParts.TryGetValue("Initial Catalog", out var catalog) && !string.IsNullOrWhiteSpace(catalog)
                ? catalog
                : null;
    }

    /// <summary>
    /// Gets the server/host name from the connection string parts.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>The server/host name if found; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
    public static string? GetServerName(this ConnectionStringConverter.ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        return info.OriginalParts.TryGetValue("Server", out var server) && !string.IsNullOrWhiteSpace(server)
            ? server
            : info.OriginalParts.TryGetValue("Host", out var host) && !string.IsNullOrWhiteSpace(host)
                ? host
                : info.OriginalParts.TryGetValue("Data Source", out var dataSource) && !string.IsNullOrWhiteSpace(dataSource)
                    ? dataSource
                    : null;
    }

    /// <summary>
    /// Gets the port number from the connection string, or null if not specified.
    /// </summary>
    /// <param name="info">The connection string information.</param>
    /// <returns>The port number as an integer if found and valid; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> is null.</exception>
    public static int? GetPort(this ConnectionStringConverter.ConnectionStringInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        if (info.OriginalParts.TryGetValue("Port", out var portStr) && !string.IsNullOrWhiteSpace(portStr))
        {
            if (int.TryParse(portStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) && port > 0 && port <= 65535)
            {
                return port;
            }
        }

        return null;
    }
}