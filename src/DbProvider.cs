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