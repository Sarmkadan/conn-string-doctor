using ConnStringDoctor.Providers;

namespace ConnStringDoctor;

/// <summary>
/// Central registry of <see cref="IConnectionStringProvider"/> strategies. Adding support for a
/// new database engine only requires implementing the interface and registering it here, rather
/// than touching the several switch statements this registry replaces.
/// </summary>
public static class ProviderRegistry
{
    // Sqlite and Npgsql are probed before MySql/SqlServer because their signals (file
    // extension, "ssl mode"/port 5432) are unambiguous, whereas SqlServer is the historical
    // fallback when no other provider claims the connection string.
    private static readonly IReadOnlyList<IConnectionStringProvider> ProbeOrder = new List<IConnectionStringProvider>
    {
        new SqliteConnectionStringProvider(),
        new NpgsqlConnectionStringProvider(),
        new MySqlConnectionStringProvider(),
        new SqlServerConnectionStringProvider(),
    };

    private static readonly IReadOnlyDictionary<DbProvider, IConnectionStringProvider> ByKind =
        ProbeOrder.ToDictionary(p => p.Kind);

    /// <summary>
    /// Gets all registered provider strategies, in probe order.
    /// </summary>
    public static IReadOnlyList<IConnectionStringProvider> All => ProbeOrder;

    /// <summary>
    /// Gets the provider strategy for the given <see cref="DbProvider"/> kind.
    /// </summary>
    /// <param name="kind">The provider kind to look up.</param>
    /// <returns>The matching provider strategy.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> has no registered strategy (e.g. <see cref="DbProvider.Unknown"/>).</exception>
    public static IConnectionStringProvider Get(DbProvider kind) =>
        ByKind.TryGetValue(kind, out var provider)
            ? provider
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "No provider strategy is registered for this provider kind.");

    /// <summary>
    /// Attempts to identify which registered provider can handle the given raw connection string.
    /// </summary>
    /// <param name="raw">The raw connection string to probe.</param>
    /// <returns>The first provider (in probe order) whose <see cref="IConnectionStringProvider.CanHandle"/> returns true, or null when none match.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is null or empty.</exception>
    public static IConnectionStringProvider? Detect(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);
        return ProbeOrder.FirstOrDefault(p => p.CanHandle(raw));
    }

    /// <summary>
    /// Gets the default TCP port for the given provider kind, or 0 when the provider is not
    /// network based or unknown.
    /// </summary>
    /// <param name="kind">The provider kind.</param>
    /// <returns>The default port, or 0 for <see cref="DbProvider.Unknown"/>.</returns>
    public static int DefaultPort(DbProvider kind) =>
        kind == DbProvider.Unknown ? 0 : Get(kind).DefaultPort;
}
