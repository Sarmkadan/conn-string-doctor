namespace ConnStringDoctor;

/// <summary>
/// Represents information parsed from a connection string.
/// </summary>
public class ConnectionStringInfo
{
    /// <summary>
    /// Gets or sets the database provider type.
    /// </summary>
    public DbProvider Provider { get; set; } = DbProvider.Unknown;

    /// <summary>
    /// Gets or sets the server/host name.
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    /// Gets or sets the port number.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets the collection of additional connection string properties.
    /// </summary>
    public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a string representation of the parsed connection string.
    /// </summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Provider: {Provider}");

        if (!string.IsNullOrEmpty(Server))
        {
            sb.Append($", Server: {Server}");
            if (Port.HasValue)
            {
                sb.Append($":{Port}");
            }
        }

        if (!string.IsNullOrEmpty(Database))
        {
            sb.Append($", Database: {Database}");
        }

        if (!string.IsNullOrEmpty(User))
        {
            sb.Append($", User: {User}");
        }

        if (Properties.Count > 0)
        {
            sb.Append($", Properties: {Properties.Count}");
        }

        return sb.ToString();
    }
}