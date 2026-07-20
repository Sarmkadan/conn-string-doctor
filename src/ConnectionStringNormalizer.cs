using System.Data.Common;
using System.Globalization;

namespace ConnStringDoctor;

/// <summary>
/// Provides functionality to normalize connection strings by:
/// - Converting keys to canonical forms via alias mapping
/// - Sorting keys alphabetically
/// - Applying consistent spacing
/// - Optionally redacting sensitive information
/// </summary>
public static class ConnectionStringNormalizer
{
    // Canonical key mappings (alias -> canonical)
    private static readonly IReadOnlyDictionary<string, string> _canonicalMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Server/Host aliases
        {"server", "Server"},
        {"data source", "Server"},
        {"address", "Server"},
        {"addr", "Server"},
        {"network address", "Server"},

        // Database aliases
        {"initial catalog", "Database"},
        {"database", "Database"},
        {"db", "Database"},

        // User aliases
        {"user id", "User ID"},
        {"uid", "User ID"},
        {"user", "User ID"},
        {"username", "User ID"},

        // Password aliases
        {"password", "Password"},
        {"pwd", "Password"},

        // Port
        {"port", "Port"},

        // Encryption/SSL
        {"encrypt", "Encrypt"},
        {"ssl mode", "Encrypt"},
        {"sslmode", "Encrypt"},
        {"trust server certificate", "Trust Server Certificate"},

        // Pooling
        {"pooling", "Pooling"},
        {"pool size", "Max Pool Size"},
        {"max pool size", "Max Pool Size"},
        {"min pool size", "Min Pool Size"},

        // Timeout
        {"connect timeout", "Connect Timeout"},
        {"connection timeout", "Connect Timeout"},
        {"timeout", "Connect Timeout"},
        {"command timeout", "Command Timeout"},
        {"default command timeout", "Default Command Timeout"},

        // Authentication
        {"integrated security", "Integrated Security"},
        {"trusted_connection", "Integrated Security"},

        // Other common keywords
        {"multiple active result sets", "Multiple Active Result Sets"},
        {"application intent", "Application Intent"},
        {"application name", "Application Name"},
        {"workstation id", "Workstation ID"},
        {"packet size", "Packet Size"}
    };

    /// <summary>
    /// Normalizes a connection string with canonical key names, sorted keys, and consistent spacing.
    /// </summary>
    /// <param name="connectionString">The connection string to normalize</param>
    /// <param name="redact">Whether to redact sensitive information</param>
    /// <returns>The normalized connection string</returns>
    public static string Normalize(string connectionString, bool redact = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString ?? string.Empty;
        }

        // Parse the connection string using the existing parser
        var parsed = ConnectionStringParser.Parse(connectionString);

        // Build a dictionary of key-value pairs with canonical keys
        var normalizedParts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Add Server
        if (!string.IsNullOrEmpty(parsed.Server))
        {
            normalizedParts["Server"] = parsed.Server;
            if (parsed.Port.HasValue)
            {
                normalizedParts["Server"] += $",{parsed.Port}";
            }
        }

        // Add Database
        if (!string.IsNullOrEmpty(parsed.Database))
        {
            normalizedParts["Database"] = parsed.Database;
        }

        // Add User ID
        if (!string.IsNullOrEmpty(parsed.User))
        {
            normalizedParts["User ID"] = parsed.User;
        }

        // Add Password (may be redacted)
        if (!string.IsNullOrEmpty(parsed.Password))
        {
            normalizedParts["Password"] = redact ? "***REDACTED***" : parsed.Password;
        }

        // Add other properties
        foreach (var prop in parsed.Properties)
        {
            string canonicalKey = GetCanonicalKey(prop.Key);
            normalizedParts[canonicalKey] = redact && IsSensitiveKey(canonicalKey)
                ? "***REDACTED***"
                : prop.Value;
        }

        // Build the normalized connection string with sorted keys
        return BuildConnectionString(normalizedParts);
    }

    /// <summary>
    /// Gets the canonical key name for a given key (handles aliases).
    /// </summary>
    private static string GetCanonicalKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key ?? string.Empty;
        }

        string normalizedKey = key.Trim().ToLowerInvariant();

        if (_canonicalMappings.TryGetValue(normalizedKey, out var canonical))
        {
            return canonical;
        }

        // If no mapping found, return the key as-is (but capitalize first letter of each word)
        return CapitalizeKeywords(key);
    }

    /// <summary>
    /// Capitalizes keywords in a connection string key (e.g., "user id" -> "User ID").
    /// </summary>
    private static string CapitalizeKeywords(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return key ?? string.Empty;
        }

        // Split by spaces and capitalize each word
        var parts = key.Split(new[] {' ', '-', '_'}, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..].ToLowerInvariant();
            }
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Checks if a key is sensitive and should be redacted.
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string normalized = key.ToLowerInvariant();
        return normalized.Contains("password") ||
               normalized.Contains("pwd") ||
               normalized.Contains("secret") ||
               normalized.Contains("token") ||
               normalized.Contains("key");
    }

    /// <summary>
    /// Builds a properly formatted connection string from key-value pairs.
    /// </summary>
    private static string BuildConnectionString(Dictionary<string, string> parts)
    {
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        // Use DbConnectionStringBuilder for proper formatting
        var builder = new DbConnectionStringBuilder();

        // Sort keys alphabetically for consistent output
        foreach (var kvp in parts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            // Parse server string to extract host and port if needed
            if (string.Equals(kvp.Key, "Server", StringComparison.OrdinalIgnoreCase) && kvp.Value.Contains(','))
            {
                // Server is already formatted as "host,port" from parsing
                builder[kvp.Key] = kvp.Value;
            }
            else
            {
                builder[kvp.Key] = kvp.Value;
            }
        }

        return builder.ConnectionString;
    }
}
