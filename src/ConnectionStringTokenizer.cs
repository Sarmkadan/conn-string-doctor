using System.Globalization;

namespace ConnStringDoctor;

/// <summary>
/// Shared low-level tokenizer for ADO.NET style connection strings ("key=value;key=value"),
/// handling quoted values and escaped semicolons. Used by <see cref="IConnectionStringProvider"/>
/// implementations so the splitting/quoting rules live in exactly one place.
/// </summary>
public static class ConnectionStringTokenizer
{
    /// <summary>
    /// Splits a connection string into trimmed "key=value" segments, honoring double/single
    /// quoted values and backslash-escaped characters.
    /// </summary>
    /// <param name="raw">The raw connection string to split.</param>
    /// <returns>The list of non-empty "key=value" segments, in source order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="raw"/> is null.</exception>
    public static List<string> Split(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var pairs = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inDoubleQuotes = false;
        bool inSingleQuotes = false;
        bool escapeNext = false;

        foreach (char c in raw)
        {
            if (escapeNext)
            {
                current.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\')
            {
                escapeNext = true;
                continue;
            }

            if (c == '"' && !inSingleQuotes)
            {
                inDoubleQuotes = !inDoubleQuotes;
                current.Append(c);
                continue;
            }

            if (c == '\'' && !inDoubleQuotes)
            {
                inSingleQuotes = !inSingleQuotes;
                current.Append(c);
                continue;
            }

            if (c == ';' && !inDoubleQuotes && !inSingleQuotes)
            {
                AddIfNotEmpty(pairs, current);
                continue;
            }

            current.Append(c);
        }

        AddIfNotEmpty(pairs, current);
        return pairs;
    }

    /// <summary>
    /// Splits a single "key=value" segment into its key and unquoted value.
    /// </summary>
    /// <param name="pair">The "key=value" segment to split.</param>
    /// <returns>The trimmed key and value, or (null, null) when the segment has no '=' separator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pair"/> is null.</exception>
    public static (string? Key, string? Value) SplitPair(string pair)
    {
        ArgumentNullException.ThrowIfNull(pair);

        int equalsIndex = pair.IndexOf('=');
        if (equalsIndex < 0)
        {
            return (null, null);
        }

        string key = pair[..equalsIndex].Trim();
        string value = pair[(equalsIndex + 1)..].Trim();

        if (value.Length >= 2)
        {
            char first = value[0];
            char last = value[^1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
            {
                value = value[1..^1];
            }
        }

        return (key, value);
    }

    /// <summary>
    /// Extracts host and port from a value formatted as "host,port", "host:port", or "[ipv6]:port".
    /// </summary>
    /// <param name="value">The raw server/host value.</param>
    /// <param name="port">When this method returns, contains the parsed port, or null if none was present.</param>
    /// <returns>The extracted host name, with any port suffix removed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static string ExtractHostAndPort(string value, out int? port)
    {
        ArgumentNullException.ThrowIfNull(value);

        port = null;

        int commaIndex = value.IndexOf(',');
        if (commaIndex >= 0)
        {
            if (int.TryParse(value[(commaIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int commaPort))
            {
                port = commaPort;
            }
            return value[..commaIndex].Trim();
        }

        if (value.StartsWith('['))
        {
            int closeIndex = value.IndexOf(']');
            if (closeIndex > 0)
            {
                if (closeIndex + 2 < value.Length && value[closeIndex + 1] == ':' &&
                    int.TryParse(value[(closeIndex + 2)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v6Port))
                {
                    port = v6Port;
                }
                return value[1..closeIndex];
            }

            return value;
        }

        int colonIndex = value.IndexOf(':');
        if (colonIndex > 0 && colonIndex < value.Length - 1 && colonIndex == value.LastIndexOf(':') &&
            int.TryParse(value[(colonIndex + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hostPort))
        {
            port = hostPort;
            return value[..colonIndex];
        }

        return value;
    }

    private static void AddIfNotEmpty(List<string> pairs, System.Text.StringBuilder current)
    {
        var value = current.ToString().Trim();
        if (!string.IsNullOrEmpty(value))
        {
            pairs.Add(value);
        }
        current.Clear();
    }
}
