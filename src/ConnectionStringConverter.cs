using System.Data.Common;

namespace ConnStringDoctor;

/// <summary>
/// Converts connection strings between provider dialects (SQL Server, PostgreSQL, MySQL, SQLite)
/// by translating well-known keywords to their target provider equivalents.
/// </summary>
public sealed class ConnectionStringConverter
{
    /// <summary>
    /// Holds the parsed parts of a connection string together with its source provider name.
    /// </summary>
    public sealed class ConnectionStringInfo
    {
        /// <summary>Gets or sets the source provider name (e.g. "sqlserver", "postgres").</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Gets or sets the original key/value pairs of the connection string.</summary>
        public IReadOnlyDictionary<string, string> OriginalParts { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a connection string into its key/value parts using ADO.NET quoting rules.
    /// The provider is left empty; set <see cref="ConnectionStringInfo.Provider"/> before converting.
    /// </summary>
    /// <param name="connectionString">The connection string to parse; may be null or empty.</param>
    /// <returns>The parsed connection string information.</returns>
    public static ConnectionStringInfo Parse(string connectionString)
    {
        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ConnectionStringInfo { Provider = string.Empty, OriginalParts = parts.AsReadOnly() };
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (string key in builder.Keys.Cast<string>())
            {
                parts[key] = builder[key]?.ToString() ?? string.Empty;
            }
        }
        catch (ArgumentException)
        {
            // Not parseable under strict ADO.NET rules; fall back to a plain split.
            ParseSegments(connectionString, parts);
        }

        return new ConnectionStringInfo { Provider = string.Empty, OriginalParts = parts.AsReadOnly() };
    }

    /// <summary>
    /// Attempts to parse a connection string into its key/value parts using ADO.NET quoting rules.
    /// The provider is left empty; set <see cref="ConnectionStringInfo.Provider"/> before converting.
    /// </summary>
    /// <param name="connectionString">The connection string to parse; may be null or empty.</param>
    /// <param name="info">When this method returns, contains the parsed connection string information if parsing succeeded, or null if parsing failed.</param>
    /// <returns>true if the connection string was successfully parsed; otherwise, false.</returns>
    public static bool TryParse(string connectionString, out ConnectionStringInfo? info)
    {
        try
        {
            info = Parse(connectionString);
            return true;
        }
        catch
        {
            info = null;
            return false;
        }
    }

    private static void ParseSegments(string connectionString, Dictionary<string, string> parts)
    {
        foreach (var segment in connectionString.Split(';'))
        {
            var trimmed = segment.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var equalIndex = trimmed.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = trimmed[..equalIndex].Trim();
                var value = trimmed[(equalIndex + 1)..].Trim();
                parts[key] = value;
            }
        }
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

    /// <summary>
    /// Converts the parsed connection string parts to the dialect of the target provider.
    /// </summary>
    /// <param name="source">The parsed source connection string, including its provider name.</param>
    /// <param name="targetProvider">The target provider name (e.g. "postgres", "mysql", "sqlserver").</param>
    /// <returns>The conversion result with the rewritten connection string, unmapped keys, and warnings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetProvider"/> is null, empty, or whitespace.</exception>
    public ConversionResult Convert(ConnectionStringInfo source, string targetProvider)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(targetProvider))
        {
            throw new ArgumentException("Target provider cannot be null or whitespace.", nameof(targetProvider));
        }

        var target = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Same provider: pass everything through unchanged.
        if (string.Equals(source.Provider?.Trim(), targetProvider.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            foreach (var kvp in source.OriginalParts)
            {
                target[kvp.Key] = kvp.Value;
            }

            return new ConversionResult(BuildConnectionString(target), Array.Empty<string>(), Array.Empty<string>());
        }

        var unmappedKeys = new List<string>();
        var warnings = new List<string>();

        var mappings = GetMappings(source.Provider ?? string.Empty, targetProvider);

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

        HandleCommonMappings(source, target);

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

    private static void HandleCommonMappings(ConnectionStringInfo source, Dictionary<string, string> target)
    {
        // Fallbacks for widely shared keywords that the provider-specific mapping did not cover.
        // A fallback is skipped when the target already carries the value under any synonym,
        // so a mapped key (e.g. "Host") is never duplicated as "Server".
        AddFallback(source, target, "Server", "Server", "Data Source", "Host");
        AddFallback(source, target, "Database", "Database", "Initial Catalog");
        AddFallback(source, target, "User ID", "User ID", "User", "Username", "Uid");
        AddFallback(source, target, "Password", "Password", "Pwd");
        AddFallback(source, target, "Port", "Port");
        AddFallback(source, target, "Encrypt", "Encrypt", "SSL Mode", "SslMode", "Trust Server Certificate");
    }

    private static void AddFallback(ConnectionStringInfo source, Dictionary<string, string> target, string targetKey, params string[] synonyms)
    {
        if (target.ContainsKey(targetKey) || synonyms.Any(target.ContainsKey))
        {
            return;
        }

        foreach (var synonym in synonyms)
        {
            if (source.OriginalParts.TryGetValue(synonym, out var value))
            {
                target[targetKey] = value;
                return;
            }
        }
    }

    private static string BuildConnectionString(Dictionary<string, string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        // DbConnectionStringBuilder applies standard ADO.NET quoting for values
        // containing separators, quotes, or leading/trailing whitespace.
        var builder = new DbConnectionStringBuilder();
        foreach (var kvp in parts.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder[kvp.Key] = kvp.Value;
        }

        return builder.ConnectionString;
    }
}

/// <summary>
/// Represents the outcome of a connection string conversion.
/// </summary>
public sealed class ConversionResult
{
    /// <summary>Gets the converted connection string.</summary>
    public string ConnectionString { get; }

    /// <summary>Gets the source keys that had no mapping for the target provider.</summary>
    public IReadOnlyList<string> UnmappedKeys { get; }

    /// <summary>Gets the warnings produced during conversion.</summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionResult"/> class.
    /// </summary>
    /// <param name="connectionString">The converted connection string; null is treated as empty.</param>
    /// <param name="unmappedKeys">The keys that could not be mapped; null is treated as empty.</param>
    /// <param name="warnings">The conversion warnings; null is treated as empty.</param>
    public ConversionResult(string connectionString, IReadOnlyList<string> unmappedKeys, IReadOnlyList<string> warnings)
    {
        ConnectionString = connectionString ?? string.Empty;
        UnmappedKeys = unmappedKeys ?? Array.Empty<string>();
        Warnings = warnings ?? Array.Empty<string>();
    }
}
