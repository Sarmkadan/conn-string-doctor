using System.Globalization;

namespace ConnStringDoctor.Providers;

/// <summary>
/// Common plumbing shared by all <see cref="IConnectionStringProvider"/> implementations:
/// tokenizing, key-alias lookup, ADO.NET style escaping, and the generic key/value to
/// <see cref="ConnectionStringInfo"/> projection.
/// </summary>
public abstract class ConnectionStringProviderBase : IConnectionStringProvider
{
    private static readonly char[] CharsRequiringQuotes = { ';', '=', '"', '\'' };

    /// <inheritdoc />
    public abstract DbProvider Kind { get; }

    /// <inheritdoc />
    public abstract int DefaultPort { get; }

    /// <inheritdoc />
    public abstract IReadOnlyDictionary<string, string> KeyAliases { get; }

    /// <inheritdoc />
    public abstract bool CanHandle(string raw);

    /// <summary>
    /// Escapes a value so that it can be safely embedded into a connection string.
    /// Values containing separators (;, =), quotes ("'), or leading/trailing whitespace
    /// are wrapped in double quotes, with internal double quotes doubled.
    /// </summary>
    /// <param name="value">The raw value to escape; null or empty returns an empty string.</param>
    /// <returns>The escaped value.</returns>
    public virtual string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool needsQuotes = value.IndexOfAny(CharsRequiringQuotes) >= 0 ||
                            value[0] == ' ' ||
                            value[^1] == ' ';

        return needsQuotes
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }

    /// <inheritdoc />
    public virtual ConnectionStringInfo Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrEmpty(raw);

        var info = new ConnectionStringInfo { Provider = Kind };

        foreach (var pair in ConnectionStringTokenizer.Split(raw))
        {
            var (key, value) = ConnectionStringTokenizer.SplitPair(pair);
            if (key is null || value is null)
            {
                continue;
            }

            ApplyKeyValue(info, key, value);
        }

        return info;
    }

    /// <summary>
    /// Applies a single parsed key/value pair onto the connection string info, mapping it
    /// through <see cref="KeyAliases"/> to the well-known <see cref="ConnectionStringInfo"/>
    /// members (Server, Port, Database, User, Password) or, failing that, into <see cref="ConnectionStringInfo.Properties"/>.
    /// </summary>
    /// <param name="info">The connection string info being built.</param>
    /// <param name="key">The raw, unmapped key.</param>
    /// <param name="value">The raw value.</param>
    protected virtual void ApplyKeyValue(ConnectionStringInfo info, string key, string value)
    {
        string canonical = KeyAliases.TryGetValue(key.Trim().ToLowerInvariant(), out var mapped)
            ? mapped
            : key;

        switch (canonical)
        {
            case "Server":
                info.Server = ConnectionStringTokenizer.ExtractHostAndPort(value, out var hostPort);
                if (hostPort.HasValue)
                {
                    info.Port = hostPort.Value;
                }
                break;

            case "Port":
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                {
                    info.Port = port;
                }
                break;

            case "Database":
                info.Database = value;
                break;

            case "User ID":
                info.User = value;
                break;

            case "Password":
                info.Password = value;
                break;

            default:
                info.Properties[key] = value;
                break;
        }
    }
}
