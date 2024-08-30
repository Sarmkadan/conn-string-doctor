namespace ConnStringDoctor;

/// <summary>
/// Static parser for connection strings that extracts provider information and key-value pairs.
/// </summary>
public static class ConnectionStringParser
{
    private static readonly HashSet<string> _sqlServerKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "server", "data source", "address", "addr", "network address"
    };

    private static readonly HashSet<string> _databaseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "initial catalog", "database", "db"
    };

    private static readonly HashSet<string> _userKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "user id", "uid", "user", "username"
    };

    private static readonly HashSet<string> _passwordKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "pwd"
    };

    private static readonly HashSet<string> _portKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "port"
    };

    /// <summary>
    /// Parses a connection string and returns structured information.
    /// </summary>
    /// <param name="connectionString">The connection string to parse (e.g., "Server=localhost;Database=test;User Id=admin")</param>
    /// <returns>Structured connection string information</returns>
    public static ConnectionStringInfo Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
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

            ProcessKeyValue(result, key, value);
        }

        DetermineProvider(result);
        return result;
    }

    /// <summary>
    /// Splits a connection string into key=value pairs, handling quoted values and escaped semicolons.
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

        // Add the last pair
        var lastPair = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastPair))
        {
            pairs.Add(lastPair);
        }

        return pairs;
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
    /// Processes a key-value pair and adds it to the result.
    /// </summary>
    private static void ProcessKeyValue(ConnectionStringInfo result, string key, string value)
    {
        // Normalize key
        string normalizedKey = NormalizeKey(key);

        // Handle server/host
        if (_sqlServerKeywords.Contains(normalizedKey))
        {
            result.Server = ExtractHostAndPort(value, out int? port);
            if (port.HasValue)
            {
                result.Port = port.Value;
            }
        }
        // Handle database/initial catalog
        else if (_databaseKeywords.Contains(normalizedKey))
        {
            result.Database = value;
        }
        // Handle user
        else if (_userKeywords.Contains(normalizedKey))
        {
            result.User = value;
        }
        // Handle password
        else if (_passwordKeywords.Contains(normalizedKey))
        {
            result.Password = value;
        }
        // Handle port explicitly
        else if (_portKeywords.Contains(normalizedKey))
        {
            if (int.TryParse(value, out int port))
            {
                result.Port = port;
            }
        }
        // Handle other properties
        else
        {
            result.Properties[key] = value;
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
    /// Extracts host and port from a value in format "host,port" or "host:port".
    /// </summary>
    private static string ExtractHostAndPort(string value, out int? port)
    {
        port = null;

        // Check for comma-separated port
        int commaIndex = value.IndexOf(',');
        if (commaIndex >= 0)
        {
            string hostPart = value[..commaIndex];
            string portPart = value[(commaIndex + 1)..];

            if (int.TryParse(portPart, out int portValue))
            {
                port = portValue;
            }
            return hostPart;
        }

        // Check for colon-separated port (IPv6 compatible)
        int colonIndex = value.LastIndexOf(':');
        if (colonIndex > 0 && colonIndex < value.Length - 1)
        {
            // Don't match IPv6 addresses like [2001:db8::1]:port
            if (value[0] != '[')
            {
                string hostPart = value[..colonIndex];
                string portPart = value[(colonIndex + 1)..];

                if (int.TryParse(portPart, out int portValue))
                {
                    port = portValue;
                }
                return hostPart;
            }
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
        bool hasSqlServerKeywords = result.Properties.Keys.Any(k => _sqlServerKeywords.Contains(k));
        bool hasDatabaseKeywords = result.Properties.Keys.Any(k => _databaseKeywords.Contains(k));

        // Check for Sqlite by file extension
        if (hasServer && (result.Server?.EndsWith(".db", StringComparison.OrdinalIgnoreCase) == true ||
                          result.Server?.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) == true ||
                          result.Server?.EndsWith(".db3", StringComparison.OrdinalIgnoreCase) == true))
        {
            result.Provider = DbProvider.Sqlite;
            return;
        }

        // Check for PostgreSql by port (default 5432) or keywords
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

        // Check for MySql by keywords
        if (result.Properties.Keys.Any(k => k.Contains("mysql", StringComparison.OrdinalIgnoreCase)))
        {
            result.Provider = DbProvider.MySql;
            return;
        }

        // Default to SqlServer if we have server and database keywords
        if (hasServer && hasDatabase && (hasSqlServerKeywords || hasDatabaseKeywords))
        {
            result.Provider = DbProvider.SqlServer;
            return;
        }

        // Check for SqlServer by keywords
        if (hasSqlServerKeywords || hasDatabaseKeywords)
        {
            result.Provider = DbProvider.SqlServer;
            return;
        }

        // Default to unknown
        result.Provider = DbProvider.Unknown;
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
}