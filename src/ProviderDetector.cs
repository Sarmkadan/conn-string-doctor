using System.Globalization;

namespace ConnStringDoctor;

/// <summary>
/// Detects the database provider from connection string keywords and patterns.
/// </summary>
public static class ProviderDetector
{
    private static readonly HashSet<string> _sqlServerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "server", "data source", "address", "addr", "network address",
        "integrated security", "trusted_connection", "application intent"
    };

    private static readonly HashSet<string> _postgresKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "host", "server", "port", "database", "dbname", "ssl mode"
    };

    private static readonly HashSet<string> _mysqlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "server", "host", "port", "database", "db", "user", "username"
    };

    private static readonly HashSet<string> _sqliteKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ".db", ".sqlite", ".db3", ".sqlite3"
    };

    private static readonly Dictionary<string, (DbProvider Provider, int Confidence)> _keywordToProviderMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Static constructor to initialize keyword mappings.
    /// </summary>
    static ProviderDetector()
    {
        // SQL Server specific keywords
        _keywordToProviderMap["integrated security"] = (DbProvider.SqlServer, 100);
        _keywordToProviderMap["trusted_connection"] = (DbProvider.SqlServer, 100);
        _keywordToProviderMap["application intent"] = (DbProvider.SqlServer, 90);

        // PostgreSQL specific keywords
        _keywordToProviderMap["ssl mode"] = (DbProvider.PostgreSql, 100);
        _keywordToProviderMap["sslmode"] = (DbProvider.PostgreSql, 100);

        // MySQL specific keywords
        _keywordToProviderMap["user"] = (DbProvider.MySql, 80);
        _keywordToProviderMap["username"] = (DbProvider.MySql, 80);
        _keywordToProviderMap["db"] = (DbProvider.MySql, 70);
    }

    /// <summary>
    /// Infers the database provider from a connection string with a confidence score.
    /// </summary>
    /// <param name="connectionString">The connection string to analyze</param>
    /// <returns>A tuple containing the detected provider and confidence score (0-100)</returns>
    public static (DbProvider Provider, int Confidence) DetectProvider(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (DbProvider.Unknown, 0);
        }

        var result = new ConnectionStringInfo();
        var pairs = SplitConnectionString(connectionString);

        foreach (var pair in pairs)
        {
            var (key, value) = ParseKeyValuePair(pair);
            if (key == null || value == null)
            {
                continue;
            }

            ProcessKey(result, key, value);
        }

        return FinalizeDetection(result);
    }

    /// <summary>
    /// Splits a connection string into key=value pairs.
    /// </summary>
    private static List<string> SplitConnectionString(string connectionString)
    {
        var pairs = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
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

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ';' && !inQuotes)
            {
                var pair = current.ToString().Trim();
                if (!string.IsNullOrEmpty(pair))
                {
                    pairs.Add(pair);
                }
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        var lastPair = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastPair))
        {
            pairs.Add(lastPair);
        }

        return pairs;
    }

    /// <summary>
    /// Parses a single key=value pair.
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

        // Handle quoted values
        if (valuePart.StartsWith('"') && valuePart.EndsWith('"'))
        {
            valuePart = valuePart[1..^1];
        }
        else if (valuePart.StartsWith("'") && valuePart.EndsWith("'"))
        {
            valuePart = valuePart[1..^1];
        }

        return (keyPart, valuePart);
    }

    /// <summary>
    /// Processes a key-value pair to extract provider information.
    /// </summary>
    private static void ProcessKey(ConnectionStringInfo result, string key, string value)
    {
        string normalizedKey = NormalizeKey(key);
        result.Properties[normalizedKey] = value;

        // Track which keyword categories we've seen
        if (_sqlServerKeywords.Contains(normalizedKey))
        {
            result.Properties["__sqlserver_keyword_seen"] = "true";
        }
        if (_postgresKeywords.Contains(normalizedKey))
        {
            result.Properties["__postgres_keyword_seen"] = "true";
        }
        if (_mysqlKeywords.Contains(normalizedKey))
        {
            result.Properties["__mysql_keyword_seen"] = "true";
        }
        if (_sqliteKeywords.Any(ext => value.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            result.Properties["__sqlite_file_seen"] = "true";
        }
    }

    /// <summary>
    /// Normalizes key names to standard forms.
    /// </summary>
    private static string NormalizeKey(string key)
    {
        return key.ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Finalizes provider detection based on all collected information.
    /// </summary>
    private static (DbProvider Provider, int Confidence) FinalizeDetection(ConnectionStringInfo result)
    {
        // Check for explicit keyword mappings first (highest confidence)
        foreach (var kvp in _keywordToProviderMap)
        {
            if (result.Properties.ContainsKey(kvp.Key))
            {
                return kvp.Value;
            }
        }

        // Check for SQLite by file extension
        if (result.Properties.TryGetValue("server", out var server) &&
            _sqliteKeywords.Any(ext => server.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return (DbProvider.Sqlite, 100);
        }

        // Also check if database/server field contains file extension
        if (result.Properties.TryGetValue("database", out var database) &&
            _sqliteKeywords.Any(ext => database.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return (DbProvider.Sqlite, 100);
        }

        // Check for SQLite by file extension (highest priority)
        if (result.Properties.TryGetValue("server", out var serverValue) &&
            _sqliteKeywords.Any(ext => serverValue.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return (DbProvider.Sqlite, 100);
        }
        if (result.Properties.TryGetValue("database", out var databaseValue) &&
            _sqliteKeywords.Any(ext => databaseValue.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return (DbProvider.Sqlite, 100);
        }

        // Check for PostgreSQL by default port
        if (result.Properties.TryGetValue("port", out var portStr) &&
            int.TryParse(portStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) &&
            port == 5432)
        {
            return (DbProvider.PostgreSql, 95);
        }

        // Check for MySQL by default port
        if (result.Properties.TryGetValue("port", out var mysqlPortStr) &&
            int.TryParse(mysqlPortStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mysqlPort) &&
            mysqlPort == 3306)
        {
            return (DbProvider.MySql, 95);
        }

        // Check for PostgreSQL by common keywords
        bool hasPostgresKeywords = result.Properties.ContainsKey("__postgres_keyword_seen");
        bool hasServer = result.Properties.ContainsKey("server");

        if (hasPostgresKeywords && hasServer)
        {
            return (DbProvider.PostgreSql, 85);
        }

        // Check for MySQL by common keywords
        bool hasMySqlKeywords = result.Properties.ContainsKey("__mysql_keyword_seen");

        if (hasMySqlKeywords && hasServer)
        {
            return (DbProvider.MySql, 85);
        }

        // Default to SQL Server when server information is present (SQL Server syntax)
        // Only if we don't have strong signals for other providers
        bool hasPostgresPort = result.Properties.TryGetValue("port", out var pgPortStr) &&
                              int.TryParse(pgPortStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pgPort) &&
                              pgPort == 5432;
        bool hasMySqlPort = result.Properties.TryGetValue("port", out var myPortStr) &&
                           int.TryParse(myPortStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var myPort) &&
                           myPort == 3306;

        if ((hasServer || result.Properties.ContainsKey("__sqlserver_keyword_seen")) && !hasPostgresPort && !hasMySqlPort)
        {
            return (DbProvider.SqlServer, 80);
        }

        // If we have database keyword but no provider-specific clues, default to SQL Server
        if (result.Properties.ContainsKey("database") || result.Properties.ContainsKey("initial catalog"))
        {
            return (DbProvider.SqlServer, 70);
        }

        return (DbProvider.Unknown, 0);
    }

    /// <summary>
    /// Gets the default port for a given database provider.
    /// </summary>
    public static int DefaultPort(DbProvider provider)
    {
        return provider switch
        {
            DbProvider.SqlServer => 1433,
            DbProvider.PostgreSql => 5432,
            DbProvider.MySql => 3306,
            DbProvider.Sqlite => 0,
            _ => 0
        };
    }

    /// <summary>
    /// Private helper class to hold parsed connection string information.
    /// </summary>
    private class ConnectionStringInfo
    {
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
