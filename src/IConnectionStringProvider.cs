namespace ConnStringDoctor;

/// <summary>
/// Encapsulates all provider-specific knowledge (detection, key aliases, escaping, default port,
/// and parsing) behind a single strategy object, so that adding support for a new database
/// engine only requires implementing this interface rather than editing several switch
/// statements spread across the codebase.
/// </summary>
public interface IConnectionStringProvider
{
    /// <summary>
    /// Gets the database provider this strategy handles.
    /// </summary>
    DbProvider Kind { get; }

    /// <summary>
    /// Gets the default TCP port used by this provider, or 0 when the provider is not network based.
    /// </summary>
    int DefaultPort { get; }

    /// <summary>
    /// Gets the mapping from lowercase connection-string key aliases (e.g. "uid", "user") to their
    /// canonical display name (e.g. "User ID") for this provider.
    /// </summary>
    IReadOnlyDictionary<string, string> KeyAliases { get; }

    /// <summary>
    /// Determines whether the raw connection string looks like it targets this provider.
    /// </summary>
    /// <param name="raw">The raw, unparsed connection string.</param>
    /// <returns>true when the connection string contains signals specific to this provider; otherwise, false.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    bool CanHandle(string raw);

    /// <summary>
    /// Escapes a value so that it can be safely embedded into a connection string produced for
    /// this provider.
    /// </summary>
    /// <param name="value">The raw value to escape; null or empty returns an empty string.</param>
    /// <returns>The escaped value, quoted when it contains separators, quotes, or surrounding whitespace.</returns>
    string Escape(string value);

    /// <summary>
    /// Parses a raw connection string into structured information using this provider's key aliases.
    /// </summary>
    /// <param name="raw">The raw connection string to parse.</param>
    /// <returns>The structured connection string information, with <see cref="ConnectionStringInfo.Provider"/> set to <see cref="Kind"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    ConnectionStringInfo Parse(string raw);
}
