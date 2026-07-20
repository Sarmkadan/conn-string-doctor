#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConnStringDoctor;

/// <summary>
/// Diagnostic check that detects deprecated/obsolete keywords in connection strings.
/// Reports deprecated keywords with suggested replacements based on the database provider.
/// </summary>
internal sealed class DeprecatedKeywordCheck : IDiagnosticCheck
{
    /// <inheritdoc />
    public string Name => "DeprecatedKeywords";

    /// <summary>
    /// Known deprecated keywords for different database providers.
    /// </summary>
    private static readonly Dictionary<DbProvider, HashSet<string>> DeprecatedKeywords = new Dictionary<DbProvider, HashSet<string>>
    {
        // SQL Server deprecated keywords
        [DbProvider.SqlServer] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Asynchronous Processing",
            "Connection Reset",
            "Network Library",
            "Persist Security Info",
            "Packet Size",
        },

        // PostgreSQL deprecated keywords (if any)
        [DbProvider.PostgreSql] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // PostgreSQL typically uses standard keywords, but some older ones might be deprecated
            "Protocol",
        },

        // MySQL deprecated keywords
        [DbProvider.MySql] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Compress",
            "Use Compression",
        },

        // SQLite - generally no deprecated keywords as it's file-based
        [DbProvider.Sqlite] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    /// <summary>
    /// Suggested replacements for deprecated keywords.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<DbProvider, string>> Replacements = new Dictionary<string, Dictionary<DbProvider, string>>(StringComparer.OrdinalIgnoreCase)
    {
        // SQL Server replacements
        ["Asynchronous Processing"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.SqlServer] = "Remove this keyword. For read-only workloads, use 'Application Intent=ReadOnly' instead.",
        },
        ["Connection Reset"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.SqlServer] = "Remove this keyword. Connection reset behavior is handled by the driver.",
        },
        ["Network Library"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.SqlServer] = "Remove this keyword. Modern .NET uses TCP/IP by default.",
        },
        ["Persist Security Info"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.SqlServer] = "Set to 'false' for security. Sensitive information should not persist in connection strings.",
        },
        ["Packet Size"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.SqlServer] = "Remove this keyword. Use default packet size (8192 bytes).",
        },

        // MySQL replacements
        ["Compress"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.MySql] = "Remove this keyword. Compression is handled automatically by modern MySQL connectors.",
        },
        ["Use Compression"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.MySql] = "Remove this keyword. Compression is enabled by default in modern connectors.",
        },

        // PostgreSQL replacements
        ["Protocol"] = new Dictionary<DbProvider, string>
        {
            [DbProvider.PostgreSql] = "Remove this keyword. Modern PostgreSQL drivers use the latest protocol version.",
        }
    };

    /// <inheritdoc />
    public Task<DiagnosticResult> RunAsync(ConnectionStringInfo info, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(info);

        var result = new DiagnosticResult(Name);

        if (info.Provider == DbProvider.Unknown)
        {
            // Cannot check without knowing the provider
            return Task.FromResult(result);
        }

        // Get the set of deprecated keywords for this provider
        if (!DeprecatedKeywords.TryGetValue(info.Provider, out var providerKeywords))
        {
            // No deprecated keywords known for this provider
            return Task.FromResult(result);
        }

        var deprecatedFound = new List<(string Key, string? Value, string Replacement)>();

        // Check all properties against deprecated keywords
        foreach (var kvp in info.Properties)
        {
            if (providerKeywords.Contains(kvp.Key))
            {
                // Found a deprecated keyword
                string replacement = GetReplacementMessage(kvp.Key, info.Provider);
                deprecatedFound.Add((kvp.Key, kvp.Value, replacement));
            }
        }

        if (deprecatedFound.Count > 0)
        {
            var message = new System.Text.StringBuilder();
            message.Append($"Found {deprecatedFound.Count} deprecated keyword(s): ");

            foreach (var (key, value, replacement) in deprecatedFound)
            {
                message.Append($"'{key}'");
                if (!string.IsNullOrEmpty(value))
                {
                    message.Append($" (value: '{value}')");
                }
                message.Append($" -> {replacement}; ");
            }

            result.SetMessage(message.ToString());

            var details = new System.Text.StringBuilder();
            details.Append("The following deprecated keywords were found in the connection string:\n");

            foreach (var (key, value, replacement) in deprecatedFound)
            {
                details.Append($"- '{key}'");
                if (!string.IsNullOrEmpty(value))
                {
                    details.Append($" = '{value}'");
                }
                details.Append($"\n  Replacement: {replacement}\n");
            }

            result.SetDetails(details.ToString());

            // Add warnings for each deprecated keyword
            foreach (var (key, _, _) in deprecatedFound)
            {
                result.AddWarning($"Deprecated keyword '{key}' found in connection string");
            }
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets the replacement message for a deprecated keyword with the given provider.
    /// </summary>
    private static string GetReplacementMessage(string keyword, DbProvider provider)
    {
        if (Replacements.TryGetValue(keyword, out var providerMap) &&
            providerMap.TryGetValue(provider, out var message))
        {
            return message;
        }

        return $"This keyword is deprecated. Remove it or consult the provider documentation for alternatives.";
    }
}
