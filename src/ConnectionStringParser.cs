#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace ConnStringDoctor;

/// <summary>
/// Static parser for connection strings that extracts provider information and key-value pairs.
/// </summary>
public static class ConnectionStringParser
{
    private enum FieldKind
    {
        Server,
        Database,
        User,
        Password,
        Port
    }

    private static readonly Dictionary<string, FieldKind> _keywordFieldKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["server"] = FieldKind.Server,
        ["data source"] = FieldKind.Server,
        ["address"] = FieldKind.Server,
        ["addr"] = FieldKind.Server,
        ["network address"] = FieldKind.Server,
        ["initial catalog"] = FieldKind.Database,
        ["database"] = FieldKind.Database,
        ["db"] = FieldKind.Database,
        ["user id"] = FieldKind.User,
        ["uid"] = FieldKind.User,
        ["user"] = FieldKind.User,
        ["username"] = FieldKind.User,
        ["password"] = FieldKind.Password,
        ["pwd"] = FieldKind.Password,
        ["port"] = FieldKind.Port
    };

    private const int MaxConnectionStringLength = 8192;
    private const int MaxKeyValuePairCount = 256;

    /// <summary>
    /// Parses a connection string and returns structured information.
    /// </summary>
    /// <param name="connectionString">The connection string to parse (e.g., "Server=localhost;Database=test;User Id=admin")</param>
    /// <returns>Structured connection string information</returns>
    /// <exception cref="ArgumentException">Thrown when the connection string is null, empty, exceeds maximum length, or contains too many key-value pairs.</exception>
    public static ConnectionStringInfo Parse(string connectionString)
    {
        if (!TryParse(connectionString, out var result, out var error))
        {
            throw new ArgumentException(error, nameof(connectionString));
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse a connection string without throwing.
    /// </summary>
    /// <param name="connectionString">The connection string to parse (e.g., "Server=localhost;Database=test;User Id=admin")</param>
    /// <param name="result">The parsed information when parsing succeeds; otherwise <c>null</c>.</param>
    /// <param name="error">An empty string when parsing succeeds; otherwise a description of why the connection string was rejected.</param>
    /// <returns><c>true</c> if the connection string was parsed; otherwise <c>false</c>.</returns>
    public static bool TryParse(
        string connectionString,
        [NotNullWhen(true)] out ConnectionStringInfo? result,
        out string error)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            result = null;
            error = "Connection string cannot be null or empty";
            return false;
        }

        if (connectionString.Length > MaxConnectionStringLength)
        {
            result = null;
            error = $"Connection string exceeds maximum length of {MaxConnectionStringLength} characters.";
            return false;
        }

        if (!TrySplitConnectionString(connectionString, out var pairs, out error))
        {
            result = null;
            return false;
        }

        var info = new ConnectionStringInfo();

        foreach (var pair in pairs)
        {
            var (key, value) = ParseKeyValuePair(pair);
            if (key == null || value == null)
            {
                continue;
            }

            ProcessKeyValue(info, key, value);
        }

        DetermineProvider(info);
        result = info;
        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Splits a connection string into key=value pairs, handling quoted values and escaped semicolons.
    /// Returns <c>false</c> with an error message when the connection string contains too many key-value pairs.
    /// </summary>
    private static bool TrySplitConnectionString(string connectionString, out List<string> pairs, out string error)
    {
        pairs = new List<string>();
        error = string.Empty;
        var current = new StringBuilder();
        bool inQuotes = false;
        bool inSingleQuotes = false;
        bool escapeNext = false;

        for (int i = 0; i < connectionString.Length; i++)
        {
            char c = connectionString[i];

            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"' && !inSingleQuotes)
            {
                inQuotes = !inQuotes;
                current.Append(c);
                continue;
            }

            if (c == '\'' && !inQuotes)
            {
                inSingleQuotes = !inSingleQuotes;
                current.Append(c);
                continue;
            }

            if (c == ';' && !inQuotes && !inSingleQuotes)
            {
                var pair = current.ToString().Trim();
                if (!string.IsNullOrEmpty(pair))
                {
                    if (pairs.Count >= MaxKeyValuePairCount)
                    {
                        error = $"Connection string exceeds maximum of {MaxKeyValuePairCount} key-value pairs.";
                        return false;
                    }
                    pairs.Add(pair);
                }
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        // Add the last pair
        var lastPair = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastPair))
        {
            if (pairs.Count >= MaxKeyValuePairCount)
            {
                error = $"Connection string exceeds maximum of {MaxKeyValuePairCount} key-value pairs.";
                return false;
            }
            pairs.Add(lastPair);
        }

        return true;
    }

    /// <summary>
    /// Parses a single key=value pair, handling quoted values.
    /// </summary>
    private static (string? key, string? value) ParseKeyValuePair(string pair)
    {
        int equalsIndex = pair.IndexOf('=');
        if (equalsIndex < 0)
        {
            return (null, null);
        }

        string keyPart = pair[..equalsIndex].Trim();
        string valuePart = pair[(equalsIndex + 1)..].Trim();

        // Handle quoted values (both single and double quotes)
        if (valuePart.Length >= 2)
        {
            char firstChar = valuePart[0];
            char lastChar = valuePart[^1];

            if ((firstChar == '"' && lastChar == '"') || (firstChar == '\'' && lastChar == '\''))
            {
                valuePart = valuePart[1..^1];
            }
        }

        // If the key is empty after trimming, treat it as an invalid pair.
        if (string.IsNullOrWhiteSpace(keyPart))
        {
            return (null, null);
        }

        return (keyPart, valuePart);
    }

    /// <summary>
    /// Processes a key-value pair and adds it to the result.
    /// </summary>
    private static void ProcessKeyValue(ConnectionStringInfo result, string key, string value)
    {
        // Normalize key
        string normalizedKey = NormalizeKey(key);

        if (!_keywordFieldKinds.TryGetValue(normalizedKey, out FieldKind fieldKind))
        {
            result.Properties[key] = value;
            return;
        }

        switch (fieldKind)
        {
            case FieldKind.Server:
                result.Server = ExtractHostAndPort(value, out int? port);
                if (port.HasValue)
                {
                    result.Port = port.Value;
                }
                break;
            case FieldKind.Database:
                result.Database = value;
                break;
            case FieldKind.User:
                result.User = value;
                break;
            case FieldKind.Password:
                result.Password = value;
                break;
            case FieldKind.Port:
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int explicitPort))
                {
                    result.Port = explicitPort;
                }
                break;
        }
    }

    /// <summary>
    /// Normalizes key names to standard forms.
    /// </summary>
    private static string NormalizeKey(string key)
    {
        return key.Trim();
    }

    /// <summary>
    /// Extracts host and port from a value in format "host,port", "host:port", or "[ipv6]:port".
    /// </summary>
    private static string ExtractHostAndPort(string value, out int? port)
    {
        port = null;

        // Comma-separated port (SQL Server style): "host,port"
        int commaIndex = value.IndexOf(',');
        if (commaIndex >= 0)
        {
            if (int.TryParse(value[(commaIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int portValue))
            {
                port = portValue;
            }
            return value[..commaIndex].Trim();
        }

        // Bracketed IPv6 address with optional port: "[2001:db8::1]:5432"
        if (value.StartsWith('['))
        {
            int closeIndex = value.IndexOf(']');
            if (closeIndex > 0)
            {
                if (closeIndex + 2 < value.Length && value[closeIndex + 1] == ':' &&
                    int.TryParse(value[(closeIndex + 2)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v6Port))
                {
                    port = v6Port;
                }
                return value[1..closeIndex];
            }

            return value;
        }

        // Colon-separated port: only when the value contains a single colon,
        // so raw IPv6 addresses like "2001:db8::1" are left intact.
        int colonIndex = value.IndexOf(':');
        if (colonIndex > 0 && colonIndex < value.Length - 1 && colonIndex == value.LastIndexOf(':') &&
            int.TryParse(value[(colonIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostPort))
        {
            port = hostPort;
            return value[..colonIndex];
        }

        return value;
    }

    /// <summary>
    /// Determines the database provider based on available information.
    /// </summary>
    private static void DetermineProvider(ConnectionStringInfo result)
    {
        bool hasServer = !string.IsNullOrEmpty(result.Server);
        bool hasDatabase = !string.IsNullOrEmpty(result.Database);

        // Check for Sqlite by file extension
        if (hasServer && (result.Server!.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                          result.Server.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                          result.Server.EndsWith(".db3", StringComparison.OrdinalIgnoreCase)))
        {
            result.Provider = DbProvider.Sqlite;
            return;
        }

        // Check for PostgreSql by default port or keywords
        if (hasServer && result.Port == 5432)
        {
            result.Provider = DbProvider.PostgreSql;
            return;
        }

        if (hasServer && result.Properties.Keys.Any(k => k.Contains("postgre", StringComparison.OrdinalIgnoreCase)))
        {
            result.Provider = DbProvider.PostgreSql;
            return;
        }

        // Check for MySql by default port or keywords
        if (hasServer && result.Port == 3306)
        {
            result.Provider = DbProvider.MySql;
            return;
        }

        if (result.Properties.Keys.Any(k => k.Contains("mysql", StringComparison.OrdinalIgnoreCase)))
        {
            result.Provider = DbProvider.MySql;
            return;
        }

        // Default to SqlServer when server or database information is present:
        // the recognized keywords ("Server", "Initial Catalog", ...) are SQL Server syntax.
        if (hasServer || hasDatabase)
        {
            result.Provider = DbProvider.SqlServer;
            return;
        }

        result.Provider = DbProvider.Unknown;
    }

    /// <summary>
    /// Gets the default port for a given database provider.
    /// Thin facade over <see cref="ProviderRegistry.DefaultPort"/>, the single source of truth
    /// for per-provider default ports.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    /// <returns>The default port, or 0 for an unknown or file-based provider.</returns>
    public static int DefaultPort(DbProvider provider) => ProviderRegistry.DefaultPort(provider);
}
