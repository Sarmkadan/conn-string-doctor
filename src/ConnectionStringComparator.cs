using System.Data.Common;
using System.Text;

namespace ConnStringDoctor;

/// <summary>
/// Provides comparison functionality for connection strings.
/// </summary>
public static class ConnectionStringComparator
{
    /// <summary>
    /// Compares two connection strings key-by-key and returns a detailed diff.
    /// </summary>
    public static string Compare(string connectionStringA, string connectionStringB, bool redact = true, bool showAll = false)
    {
        if (string.IsNullOrWhiteSpace(connectionStringA))
        {
            throw new ArgumentException("Connection string A cannot be null or empty", nameof(connectionStringA));
        }

        if (string.IsNullOrWhiteSpace(connectionStringB))
        {
            throw new ArgumentException("Connection string B cannot be null or empty", nameof(connectionStringB));
        }

        // Parse both connection strings
        var infoA = ConnectionStringParser.Parse(connectionStringA);
        var infoB = ConnectionStringParser.Parse(connectionStringB);

        // Redact if requested
        var dictA = redact ? ConnectionStringRedactor.RedactToDictionary(connectionStringA) : ParseToDictionary(connectionStringA);
        var dictB = redact ? ConnectionStringRedactor.RedactToDictionary(connectionStringB) : ParseToDictionary(connectionStringB);

        var builder = new StringBuilder();
        builder.AppendLine("=== Connection String Comparison ===");
        builder.AppendLine();

        // Header with basic info
        builder.AppendLine("Connection String A:");
        builder.AppendLine($"  Original: {Truncate(connectionStringA, 80)}");
        builder.AppendLine($"  Provider: {infoA.Provider}");
        builder.AppendLine($"  Server: {infoA.Server}{(infoA.Port.HasValue ? $":{infoA.Port}" : "")}");
        builder.AppendLine($"  Database: {infoA.Database}");
        builder.AppendLine($"  User: {infoA.User}");
        builder.AppendLine();

        builder.AppendLine("Connection String B:");
        builder.AppendLine($"  Original: {Truncate(connectionStringB, 80)}");
        builder.AppendLine($"  Provider: {infoB.Provider}");
        builder.AppendLine($"  Server: {infoB.Server}{(infoB.Port.HasValue ? $":{infoB.Port}" : "")}");
        builder.AppendLine($"  Database: {infoB.Database}");
        builder.AppendLine($"  User: {infoB.User}");
        builder.AppendLine();

        // Compare keys
        var allKeys = dictA.Keys.Concat(dictB.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        allKeys.Sort(StringComparer.OrdinalIgnoreCase);

        builder.AppendLine("=== Key-by-Key Comparison ===");
        builder.AppendLine();

        bool hasDifferences = false;
        
        foreach (var key in allKeys)
        {
            var valueA = dictA.TryGetValue(key, out var v) ? v : null;
            var valueB = dictB.TryGetValue(key, out var v2) ? v2 : null;

            bool inA = dictA.ContainsKey(key);
            bool inB = dictB.ContainsKey(key);

            if (!showAll && !inA && !inB)
            {
                continue;
            }

            if (!hasDifferences && (inA != inB || valueA != valueB))
            {
                hasDifferences = true;
            }

            if (inA && inB)
            {
                // Key exists in both
                if (valueA == valueB)
                {
                    if (showAll)
                    {
                        builder.AppendLine($"✓ {key} = {FormatValue(valueA)}");
                    }
                }
                else
                {
                    builder.AppendLine($"✗ {key}");
                    builder.AppendLine($"  A: {FormatValue(valueA)}");
                    builder.AppendLine($"  B: {FormatValue(valueB)}");
                }
            }
            else if (inA)
            {
                // Only in A
                builder.AppendLine($"⊖ {key} (only in A)");
                builder.AppendLine($"  A: {FormatValue(valueA)}");
                builder.AppendLine("  B: <not present>");
            }
            else
            {
                // Only in B
                builder.AppendLine($"⊕ {key} (only in B)");
                builder.AppendLine("  A: <not present>");
                builder.AppendLine($"  B: {FormatValue(valueB)}");
            }
        }

        if (!hasDifferences && !showAll)
        {
            builder.AppendLine("No differences found (use --show-all to see all keys)");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Parses a connection string to a dictionary without redacting.
    /// </summary>
    private static Dictionary<string, string> ParseToDictionary(string connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return result;
        }

        try
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (string key in builder.Keys)
            {
                result[key] = builder[key]?.ToString() ?? string.Empty;
            }
        }
        catch (ArgumentException)
        {
            // If parsing fails, return empty dictionary
        }
        
        return result;
    }

    /// <summary>
    /// Formats a value for display, truncating long values.
    /// </summary>
    private static string FormatValue(string? value)
    {
        if (value == null)
        {
            return "<null>";
        }

        if (value.Length > 100)
        {
            return $"{value.Substring(0, 97)}... ({value.Length} chars)";
        }

        return value;
    }

    /// <summary>
    /// Truncates a string if it's too long.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength - 3) + "...";
    }
}
