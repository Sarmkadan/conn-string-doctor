using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that detects unknown keywords in connection strings and suggests
/// the closest known keyword using Levenshtein distance.
/// </summary>
internal sealed class UnknownKeywordCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "UnknownKeywords";

    /// <summary>
    /// All known keywords organized by database provider.
    /// </summary>
    private static readonly Dictionary<DbProvider, HashSet<string>> KnownKeywords = new Dictionary<DbProvider, HashSet<string>>
    {
        // SQL Server known keywords
        [DbProvider.SqlServer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "server", "data source", "address", "addr", "network address",
            "initial catalog", "database", "db",
            "user id", "uid", "user", "username",
            "password", "pwd",
            "port",
            "integrated security", "trusted_connection",
            "application intent",
            "connect timeout", "connection timeout",
            "encrypt",
            "trustservercertificate",
            "multipleactiveresultsets",
            "pooling", "max pool size", "min pool size",
            "asynchronous processing",
            "connection reset",
            "network library",
            "persist security info",
            "packet size"
        },

        // PostgreSQL known keywords
        [DbProvider.PostgreSql] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "hostname", "server",
            "port",
            "database", "dbname",
            "user", "username", "user id",
            "password", "pwd",
            "sslmode",
            "sslcert",
            "sslkey",
            "sslrootcert",
            "connect timeout", "connection timeout",
            "application name",
            "search_path",
            "protocol"
        },

        // MySQL known keywords
        [DbProvider.MySql] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host", "server", "hostname",
            "port",
            "database", "db",
            "user", "username", "user id",
            "password", "pwd",
            "sslmode",
            "connect timeout", "connection timeout",
            "compress", "use compression",
            "allowpublickeyretrieval",
            "charset", "character set"
        },

        // SQLite known keywords (file-based, so mostly just the database file path)
        [DbProvider.Sqlite] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // SQLite connection strings typically just have the database file path
            // but may include some configuration parameters
            "mode",
            "cache",
            "synchronous",
            "temp_store"
        }
    };

    /// <summary>
    /// Levenshtein distance calculator for string similarity.
    /// </summary>
    private static class Levenshtein
    {
        /// <summary>
        /// Calculates the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="a">First string</param>
        /// <param name="b">Second string</param>
        /// <returns>The Levenshtein distance (number of edits needed to transform a to b)</returns>
        public static int Distance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] distance = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                distance[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                distance[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1,      // deletion
                               distance[i, j - 1] + 1),     // insertion
                        distance[i - 1, j - 1] + cost);   // substitution
                }
            }

            return distance[a.Length, b.Length];
        }

        /// <summary>
        /// Finds the closest match for a given keyword among a collection of keywords.
        /// </summary>
        /// <param name="keyword">The keyword to find a match for</param>
        /// <param name="candidates">Collection of candidate keywords</param>
        /// <param name="maxDistance">Maximum acceptable distance (default: 3)</param>
        /// <returns>Tuple of (closestKeyword, distance) or (null, -1) if no good match found</returns>
        public static (string? keyword, int distance) FindClosestMatch(string keyword, IEnumerable<string> candidates, int maxDistance = 3)
        {
            if (string.IsNullOrEmpty(keyword) || candidates == null || !candidates.Any())
                return (null, -1);

            string normalizedKeyword = keyword.ToLowerInvariant().Trim();

            // Try exact match first
            if (candidates.Contains(normalizedKeyword, StringComparer.OrdinalIgnoreCase))
                return (keyword, 0);

            // Find closest match using Levenshtein distance
            string? closest = null;
            int minDistance = int.MaxValue;

            foreach (var candidate in candidates)
            {
                int distance = Distance(normalizedKeyword, candidate.ToLowerInvariant().Trim());

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = candidate;
                }
            }

            // Return match only if within acceptable distance
            if (minDistance <= maxDistance)
                return (closest, minDistance);

            return (null, -1);
        }
    }

    /// <inheritdoc />
    public Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        // Cannot check without knowing the provider
        if (info.Provider == DbProvider.Unknown)
        {
            return Task.FromResult(result);
        }

        // Get the set of known keywords for this provider
        if (!KnownKeywords.TryGetValue(info.Provider, out var providerKeywords))
        {
            // No known keywords for this provider
            return Task.FromResult(result);
        }

        var unknownKeywordsFound = new List<(string Key, string? SuggestedReplacement, int Distance)>();

        // Check all properties against known keywords
        foreach (var kvp in info.Properties)
        {
            string normalizedKey = kvp.Key.ToLowerInvariant().Trim();

            // Check if this is a known keyword
            if (!providerKeywords.Contains(normalizedKey, StringComparer.OrdinalIgnoreCase))
            {
                // Found an unknown keyword - suggest closest match
                var (suggested, distance) = Levenshtein.FindClosestMatch(kvp.Key, providerKeywords);

                if (suggested != null)
                {
                    unknownKeywordsFound.Add((kvp.Key, suggested, distance));
                }
            }
        }

        if (unknownKeywordsFound.Count > 0)
        {
            var message = new System.Text.StringBuilder();
            message.Append($"Found {unknownKeywordsFound.Count} unknown keyword(s): ");

            foreach (var (key, suggested, distance) in unknownKeywordsFound)
            {
                message.Append($"'{key}'");
                if (!string.IsNullOrEmpty(suggested))
                {
                    message.Append($" -> '{suggested}' (distance: {distance})");
                }
                message.Append("; ");
            }

            result.SetMessage(message.ToString());

            var details = new System.Text.StringBuilder();
            details.Append("The following unknown keywords were found in the connection string:\n");

            foreach (var (key, suggested, distance) in unknownKeywordsFound)
            {
                details.Append($"- '{key}'");
                if (!string.IsNullOrEmpty(suggested))
                {
                    details.Append($" -> Suggested replacement: '{suggested}' (Levenshtein distance: {distance})");
                }
                details.Append("\n");
            }

            result.SetDetails(details.ToString());

            // Add warnings for each unknown keyword with suggestion
            foreach (var (key, suggested, distance) in unknownKeywordsFound)
            {
                if (suggested != null)
                {
                    result.AddWarning($"Unknown keyword '{key}' found in connection string. Did you mean '{suggested}'? (distance: {distance})");
                }
                else
                {
                    result.AddWarning($"Unknown keyword '{key}' found in connection string. No close match found in known keywords.");
                }
            }
        }

        return Task.FromResult(result);
    }
}