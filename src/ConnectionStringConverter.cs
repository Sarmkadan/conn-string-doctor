namespace ConnStringDoctor;

public sealed class ConnectionStringConverter
{
    public sealed class ConnectionStringInfo
    {
        public string Provider { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> OriginalParts { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public static ConnectionStringInfo Parse(string connectionString)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var provider = string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ConnectionStringInfo { Provider = provider, OriginalParts = parts.AsReadOnly() };
        }

        var segments = connectionString.Split(';');
        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var equalIndex = trimmed.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = trimmed.Substring(0, equalIndex).Trim();
                var value = trimmed.Substring(equalIndex + 1).Trim();
                parts[key] = value;
            }
        }

        return new ConnectionStringInfo { Provider = provider, OriginalParts = parts.AsReadOnly() };
    }

    private static readonly IReadOnlyDictionary<string, string> SqlServerToPostgres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Server"] = "Host",
        ["Data Source"] = "Host",
        ["Database"] = "Database",
        ["Initial Catalog"] = "Database",
        ["User ID"] = "Username",
        ["User"] = "Username",
        ["Password"] = "Password",
        ["Port"] = "Port",
        ["Encrypt"] = "SSL Mode",
        ["Trust Server Certificate"] = "SSL Mode"
    };

    private static readonly IReadOnlyDictionary<string, string> PostgresToSqlServer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Host"] = "Server",
        ["Database"] = "Database",
        ["Username"] = "User ID",
        ["Password"] = "Password",
        ["Port"] = "Port",
        ["SSL Mode"] = "Encrypt"
    };

    private static readonly IReadOnlyDictionary<string, string> SqlServerToMySql = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Server"] = "Server",
        ["Data Source"] = "Server",
        ["Database"] = "Database",
        ["Initial Catalog"] = "Database",
        ["User ID"] = "Uid",
        ["User"] = "Uid",
        ["Password"] = "Password",
        ["Port"] = "Port",
        ["Encrypt"] = "SslMode"
    };

    private static readonly IReadOnlyDictionary<string, string> MySqlToSqlServer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Server"] = "Server",
        ["Database"] = "Database",
        ["Uid"] = "User ID",
        ["Password"] = "Password",
        ["Port"] = "Port",
        ["SslMode"] = "Encrypt"
    };

    private static readonly IReadOnlyDictionary<string, string> SqliteToGeneric = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Data Source"] = "Data Source",
        ["Mode"] = "Mode"
    };

    private static readonly IReadOnlyDictionary<string, string> GenericToSqlite = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Data Source"] = "Data Source",
        ["Mode"] = "Mode"
    };

    public ConversionResult Convert(ConnectionStringInfo source, string targetProvider)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrWhiteSpace(targetProvider))
        {
            throw new ArgumentException("Target provider cannot be null or whitespace.", nameof(targetProvider));
        }

        var target = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var unmappedKeys = new List<string>();
        var warnings = new List<string>();

        var mappings = GetMappings(source.Provider, targetProvider);

        foreach (var kvp in source.OriginalParts)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            if (mappings.TryGetValue(key, out var targetKey))
            {
                target[targetKey] = value;
            }
            else
            {
                unmappedKeys.Add(key);
                warnings.Add($"Key '{key}' was not mapped to target provider '{targetProvider}'");
            }
        }

        HandleCommonMappings(source, target, warnings);

        var connectionString = BuildConnectionString(target);

        return new ConversionResult(
            connectionString,
            unmappedKeys.AsReadOnly(),
            warnings.AsReadOnly()
        );
    }

    private static IReadOnlyDictionary<string, string> GetMappings(string sourceProvider, string targetProvider)
    {
        sourceProvider = (sourceProvider ?? string.Empty).Trim();
        targetProvider = (targetProvider ?? string.Empty).Trim();

        if (string.Equals(sourceProvider, targetProvider, StringComparison.OrdinalIgnoreCase))
        {
            return new Dictionary<string, string>();
        }

        if (IsSqlServerProvider(sourceProvider) && IsPostgresProvider(targetProvider))
        {
            return SqlServerToPostgres;
        }

        if (IsPostgresProvider(sourceProvider) && IsSqlServerProvider(targetProvider))
        {
            return PostgresToSqlServer;
        }

        if (IsSqlServerProvider(sourceProvider) && IsMySqlProvider(targetProvider))
        {
            return SqlServerToMySql;
        }

        if (IsMySqlProvider(sourceProvider) && IsSqlServerProvider(targetProvider))
        {
            return MySqlToSqlServer;
        }

        if (IsSqliteProvider(sourceProvider) && IsGenericProvider(targetProvider))
        {
            return SqliteToGeneric;
        }

        if (IsGenericProvider(sourceProvider) && IsSqliteProvider(targetProvider))
        {
            return GenericToSqlite;
        }

        return new Dictionary<string, string>();
    }

    private static bool IsSqlServerProvider(string provider)
    {
        return string.Equals(provider, "sqlserver", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "mssql", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "sql server", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPostgresProvider(string provider)
    {
        return string.Equals(provider, "postgres", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "postgresql", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "npgsql", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMySqlProvider(string provider)
    {
        return string.Equals(provider, "mysql", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "mariadb", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSqliteProvider(string provider)
    {
        return string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(provider, "sqlitepclraw", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenericProvider(string provider)
    {
        return !IsSqlServerProvider(provider) &&
               !IsPostgresProvider(provider) &&
               !IsMySqlProvider(provider) &&
               !IsSqliteProvider(provider);
    }

    private static void HandleCommonMappings(ConnectionStringInfo source, Dictionary<string, string> target, List<string> warnings)
    {
        if (!target.ContainsKey("Server") && source.OriginalParts.TryGetValue("Server", out var serverValue))
        {
            target["Server"] = serverValue;
        }
        else if (!target.ContainsKey("Server") && source.OriginalParts.TryGetValue("Host", out var hostValue))
        {
            target["Server"] = hostValue;
        }

        if (!target.ContainsKey("Database") && source.OriginalParts.TryGetValue("Database", out var dbValue))
        {
            target["Database"] = dbValue;
        }
        else if (!target.ContainsKey("Database") && source.OriginalParts.TryGetValue("Initial Catalog", out var catalogValue))
        {
            target["Database"] = catalogValue;
        }

        if (!target.ContainsKey("User ID") && source.OriginalParts.TryGetValue("User ID", out var userIdValue))
        {
            target["User ID"] = userIdValue;
        }
        else if (!target.ContainsKey("User ID") && source.OriginalParts.TryGetValue("User", out var userValue))
        {
            target["User ID"] = userValue;
        }
        else if (!target.ContainsKey("User ID") && source.OriginalParts.TryGetValue("Username", out var usernameValue))
        {
            target["User ID"] = usernameValue;
        }

        if (!target.ContainsKey("Password") && source.OriginalParts.TryGetValue("Password", out var passwordValue))
        {
            target["Password"] = passwordValue;
        }

        if (!target.ContainsKey("Port") && source.OriginalParts.TryGetValue("Port", out var portValue))
        {
            target["Port"] = portValue;
        }

        if (!target.ContainsKey("Encrypt") && source.OriginalParts.TryGetValue("Encrypt", out var encryptValue))
        {
            target["Encrypt"] = encryptValue;
        }
        else if (!target.ContainsKey("Encrypt") && source.OriginalParts.TryGetValue("SSL Mode", out var sslModeValue))
        {
            target["Encrypt"] = sslModeValue;
        }
        else if (!target.ContainsKey("Encrypt") && source.OriginalParts.TryGetValue("Trust Server Certificate", out var trustCertValue))
        {
            target["Encrypt"] = trustCertValue;
        }
    }

    private static string BuildConnectionString(Dictionary<string, string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        var first = true;

        foreach (var kvp in parts.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!first)
            {
                sb.Append(';');
            }
            first = false;

            sb.Append(kvp.Key).Append('=').Append(EscapeValue(kvp.Value));
        }

        return sb.ToString();
    }

    private static string EscapeValue(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("=", "\\=").Replace(";", "\\;");
    }
}

public sealed class ConversionResult
{
    public string ConnectionString { get; }
    public IReadOnlyList<string> UnmappedKeys { get; }
    public IReadOnlyList<string> Warnings { get; }

    public ConversionResult(string connectionString, IReadOnlyList<string> unmappedKeys, IReadOnlyList<string> warnings)
    {
        ConnectionString = connectionString ?? string.Empty;
        UnmappedKeys = unmappedKeys ?? Array.Empty<string>();
        Warnings = warnings ?? Array.Empty<string>();
    }
}
