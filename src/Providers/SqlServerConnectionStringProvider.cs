namespace ConnStringDoctor.Providers;

/// <summary>
/// Provider strategy for Microsoft SQL Server connection strings.
/// </summary>
public sealed class SqlServerConnectionStringProvider : ConnectionStringProviderBase
{
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["server"] = "Server",
        ["data source"] = "Server",
        ["address"] = "Server",
        ["addr"] = "Server",
        ["network address"] = "Server",
        ["initial catalog"] = "Database",
        ["database"] = "Database",
        ["user id"] = "User ID",
        ["uid"] = "User ID",
        ["user"] = "User ID",
        ["username"] = "User ID",
        ["password"] = "Password",
        ["pwd"] = "Password",
        ["port"] = "Port",
        ["integrated security"] = "Integrated Security",
        ["trusted_connection"] = "Integrated Security",
        ["application intent"] = "Application Intent",
    };

    /// <inheritdoc />
    public override DbProvider Kind => DbProvider.SqlServer;

    /// <inheritdoc />
    public override int DefaultPort => 1433;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, string> KeyAliases => Aliases;

    /// <summary>
    /// Determines whether the connection string carries SQL Server specific signals
    /// (Integrated Security, Trusted_Connection, or Application Intent).
    /// </summary>
    /// <param name="raw">The raw connection string.</param>
    /// <returns>true when a SQL Server specific keyword is present; otherwise, false.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    public override bool CanHandle(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);

        foreach (var pair in ConnectionStringTokenizer.Split(raw))
        {
            var (key, _) = ConnectionStringTokenizer.SplitPair(pair);
            if (key is null)
            {
                continue;
            }

            string normalized = key.Trim().ToLowerInvariant();
            if (normalized is "integrated security" or "trusted_connection" or "application intent")
            {
                return true;
            }
        }

        return false;
    }
}
