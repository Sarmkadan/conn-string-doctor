namespace ConnStringDoctor.Providers;

/// <summary>
/// Provider strategy for SQLite connection strings, identified by a file-based data source
/// rather than a network server.
/// </summary>
public sealed class SqliteConnectionStringProvider : ConnectionStringProviderBase
{
    private static readonly string[] FileExtensions = { ".db", ".sqlite", ".db3", ".sqlite3" };

    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["data source"] = "Server",
        ["datasource"] = "Server",
        ["filename"] = "Server",
        ["mode"] = "Mode",
    };

    /// <inheritdoc />
    public override DbProvider Kind => DbProvider.Sqlite;

    /// <inheritdoc />
    public override int DefaultPort => 0;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> KeyAliases => Aliases;

    /// <summary>
    /// Determines whether the connection string points at a SQLite database file, recognized by
    /// its "Data Source"/"Filename" value ending in a known SQLite file extension.
    /// </summary>
    /// <param name="raw">The raw connection string.</param>
    /// <returns>true when a SQLite file extension is present; otherwise, false.</returns>
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
            if (normalized is "data source" or "datasource" or "filename" or "server" &&
                FileExtensions.Any(ext => value.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
