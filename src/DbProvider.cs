using System;
using System.Collections.Generic;

namespace ConnStringDoctor;

/// <summary>
/// Database provider types.
/// </summary>
public enum DbProvider
{
    /// <summary>
    /// Unknown provider type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Microsoft SQL Server.
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// MySQL.
    /// </summary>
    MySql = 3,

    /// <summary>
    /// SQLite.
    /// </summary>
    Sqlite = 4
}

/// <summary>
/// Holds static lookup tables for provider metadata to avoid per‑call dictionary/list construction.
/// </summary>
public static class DbProviderMetadata
{
    /// <summary>
    /// Mapping from provider name (case‑insensitive) to <see cref="DbProvider"/> enum value.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, DbProvider> NameToProvider =
        new Dictionary<string, DbProvider>(StringComparer.OrdinalIgnoreCase)
        {
            { "sqlserver", DbProvider.SqlServer },
            { "mssql", DbProvider.SqlServer },
            { "postgresql", DbProvider.PostgreSql },
            { "postgres", DbProvider.PostgreSql },
            { "mysql", DbProvider.MySql },
            { "sqlite", DbProvider.Sqlite }
        };

    /// <summary>
    /// Mapping from <see cref="DbProvider"/> enum value to its canonical provider name.
    /// </summary>
    public static readonly IReadOnlyDictionary<DbProvider, string> ProviderToName =
        new Dictionary<DbProvider, string>
        {
            { DbProvider.SqlServer, "sqlserver" },
            { DbProvider.PostgreSql, "postgresql" },
            { DbProvider.MySql, "mysql" },
            { DbProvider.Sqlite, "sqlite" },
            { DbProvider.Unknown, "unknown" }
        };

    /// <summary>
    /// Tries to parse a provider name into a <see cref="DbProvider"/> value.
    /// </summary>
    /// <param name="name">The provider name.</param>
    /// <param name="provider">The parsed enum value if successful.</param>
    /// <returns>True if the name could be parsed; otherwise false.</returns>
    public static bool TryParse(string? name, out DbProvider provider)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            provider = DbProvider.Unknown;
            return false;
        }

        return NameToProvider.TryGetValue(name, out provider);
    }

    /// <summary>
    /// Gets the canonical name for a <see cref="DbProvider"/> value.
    /// </summary>
    /// <param name="provider">The provider enum.</param>
    /// <returns>The canonical provider name, or "unknown" if not found.</returns>
    public static string GetName(DbProvider provider)
    {
        return ProviderToName.TryGetValue(provider, out var name) ? name : "unknown";
    }
}
